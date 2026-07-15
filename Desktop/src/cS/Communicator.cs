using Renci.SshNet;
using Renci.SshNet.Messages.Connection;

using mLogger;

//
// Manages and SSH connection to a Cluster Hub, and tunnels to individual Nodes for file operations and command execution.
public class mCommunicator : IDisposable
{    
    private class NodeInfo
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public int LocalPort { get; set; }
        public ForwardedPortLocal Port { get; set; }
        public SftpClient Sftp { get; set; }
    }

    public readonly string _host;
    public readonly string _username;
    private readonly string _password;

    private SshClient _sshClient;
    private readonly Dictionary<string, NodeInfo> _nodeConnections = new();
   
    private readonly object _lock = new object();

    //private readonly RichTextBox _outputBox;
    //private readonly Action<string> _logAction;

    public bool IsConnected => _sshClient != null && _sshClient.IsConnected;

    Logger logger = Logger.Instance;

    //Constructor/Destructor
    public mCommunicator(string host, string username, string password) //, RichTextBox outputBox = null, Action<string> logAction = null)
    {
        _host = host;
        _username = username;
        _password = password;
        //_outputBox = outputBox;
        //_logAction = logAction;
    }
    public void Dispose()
    {
        Disconnect();
    }

    //Connection Methods
    public bool Connect()
    {
        bool lockTaken = false;
        try
        {
            Monitor.TryEnter(_lock, TimeSpan.FromSeconds(2), ref lockTaken);
            if (!lockTaken)
            {
                logger.Log(LogLevel.WARN, "mCommunicator", "Could not acquire connection lock");
                return false;
            }
            lock (_lock)
            {
                if (IsConnected) return true;

                // Initialize SSH client and node tunnels
                _sshClient = new SshClient(_host, _username, _password);
                try
                {
                    MessageBox.Show("Connecting to Cluster Hub at " + _username + "@" + _host + ". Please wait...");
                    _sshClient.Connect();
                    RebuildNodeTunnels();
                    logger.Log(LogLevel.INFO, "mCommunicator", $"Connected to {_host}");

                    return true;
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.ERROR, "mCommunicator", $"Connection error: {ex.Message}");
                    MessageBox.Show("Could not connect to " + _username + "@" + _host + ".");
                    _sshClient = null;

                    return false;
                }
            }

        }
        finally
        {
            if (lockTaken) Monitor.Exit(_lock);
        }

    }
    public void Disconnect()
    {
        lock (_lock)
        {
            foreach (NodeInfo node in _nodeConnections.Values)
            {
                if (node.Sftp.IsConnected) node.Sftp.Disconnect();
                node.Sftp.Dispose();
                node.Port.Stop();
            }
            _nodeConnections.Clear();

            if (_sshClient != null && _sshClient.IsConnected)
            {
                _sshClient.Disconnect();
                logger.Log(LogLevel.INFO, "mCommunicator", "SSH client disconnected");
            }

            _sshClient?.Dispose();
            _sshClient = null;
        }
    }

    public bool AddNodeSFTP(string nodeHost, string nodeUsername, bool verbose = true)
    {
        if (!IsConnected)
        {
            Connect();
            if (!IsConnected)
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "Failed to connect before initializing Node SFTP.");
                throw new InvalidOperationException("Failed to connect before initializing Node SFTP.");
            }
        }

        if (_nodeConnections.ContainsKey(nodeHost))
            return true; // Already initialized

        try
        {
            // Pick a unique local port per node (2222, 2223, etc.)
            int localPort = 2200 + _nodeConnections.Count;

            var port = new ForwardedPortLocal("127.0.0.1", (uint)localPort, nodeHost, 22);
            _sshClient.AddForwardedPort(port);
            port.Start();
            if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Forwarded port created for Node {nodeHost}: localhost:{localPort}");

            var sftp = new SftpClient("127.0.0.1", localPort, nodeUsername, _password);
            sftp.Connect();
            if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"SFTP connection established to Node {nodeHost} via forwarded port");

            //_nodeConnections[nodeHost] = (port, sftp);
            _nodeConnections[nodeHost] = new NodeInfo
            {
                Host = nodeHost,
                Username = nodeUsername,
                LocalPort = localPort,
                Port = port,
                Sftp = sftp
            };

            if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Initialized SFTP tunnel for {nodeHost}");
            return true;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to initialize Node SFTP for {nodeHost}: {ex.Message}");
            return false;
        }
    }
    private void RebuildNodeTunnels(bool verbose = false)
    {
        foreach (var node in _nodeConnections.Values)
        {
            try
            {
                if (node.Port != null)
                {
                    node.Port.Stop();
                    _sshClient.RemoveForwardedPort(node.Port);
                }

                var newPort = new ForwardedPortLocal("127.0.0.1", (uint)node.LocalPort, node.Host, 22);
                _sshClient.AddForwardedPort(newPort);
                newPort.Start();

                node.Port = newPort;

                node.Sftp?.Dispose();
                node.Sftp = new SftpClient("127.0.0.1", node.LocalPort, node.Username, _password);
                node.Sftp.Connect();

                if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Rebuilt tunnel and SFTP for {node.Host}");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to rebuild tunnel for {node.Host}: {ex.Message}");
            }
        }
    }

    //Hub File Methods
    public bool HubFileExists(string hubFilePath, bool verbose = false)
    {
        try
        {
            string result = ExecuteHubCommand($"[ -f \"{hubFilePath}\" ] && echo \"exists\" || echo \"missing\"", verbose);
            if (result.Contains("exists"))
            {
                if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Hub file exists: {hubFilePath}");
            }
            else
            {
                logger.Log(LogLevel.INFO, "mCommunicator", $"Hub file missing: {hubFilePath}");
            }
            return result.Contains("exists");
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Error checking existence of {hubFilePath}: {ex.Message}");
            throw;
        }
    }
    public DateTime HubFileLastModified(string pathVariable)
    {
        if (string.IsNullOrWhiteSpace(pathVariable))
            throw new ArgumentException("Path cannot be null or empty.", nameof(pathVariable));

        // Escape the path in case it contains spaces or special characters
        string escapedPath = pathVariable.Replace("\"", "\\\"");
        string command = $"stat -c %Y \"{escapedPath}\""; // %Y = epoch time (seconds since 1970-01-01)

        string result = ExecuteHubCommand(command);

        if (string.IsNullOrWhiteSpace(result))
            throw new FileNotFoundException($"No result returned for path: {pathVariable}");

        if (!long.TryParse(result.Trim(), out long epochSeconds))
            throw new FormatException($"Unexpected response from stat command: '{result}'");

        // Convert from Unix epoch seconds to local DateTime
        DateTime lastModified = DateTimeOffset.FromUnixTimeSeconds(epochSeconds).LocalDateTime;

        return lastModified;
    }

    public bool DeleteHubFile(string hubFilePath, bool verbose = true)
    {
        try
        {
            // Check if file exists before deletion
            if (!HubFileExists(hubFilePath, false))
            {
                logger.Log(LogLevel.WARN, "mCommunicator", $"File not found before deletion: {hubFilePath}");
                return false;
            }

            // Attempt deletion
            ExecuteHubCommand($"rm -f \"{hubFilePath}\"", false);

            // Verify deletion
            if (!HubFileExists(hubFilePath, false))
            {
                logger.Log(LogLevel.INFO, "mCommunicator", $"Successfully deleted {hubFilePath}");
                return true;
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to delete {hubFilePath}");
                return false;
            }

        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Error deleting {hubFilePath}: {ex.Message}");
            throw;
        }
    }
    public bool MoveHubFile(string currentFilePath, string newFilePath, bool verbose = false)
    {
        try
        {
            // Check if source file exists
            if (!HubFileExists(currentFilePath, verbose))
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Source file not found: {currentFilePath}");
                return false;
            }

            // Check if target already exists
            if (HubFileExists(newFilePath, verbose))
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Target file already exists: {newFilePath}");
                return false;
            }

            // Attempt rename
            ExecuteHubCommand($"mv \"{currentFilePath}\" \"{newFilePath}\"", verbose);

            // Verify rename
            if (!HubFileExists(currentFilePath, verbose) || HubFileExists(newFilePath, verbose))
            {
                if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Successfully renamed {currentFilePath} → {newFilePath}");
                return true;
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to rename {currentFilePath} → {newFilePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Error renaming {currentFilePath} → {newFilePath}: {ex.Message}");
            throw;
        }
    }

    //Node File Methods
    public bool NodeFileExists(string nodeFilePath, string host, bool verbose = false)
    {
        if (!_nodeConnections.TryGetValue(host, out var node))
            throw new InvalidOperationException($"Node {host} not initialized.");

        try
        {
            if (node.Sftp == null || !node.Sftp.IsConnected)
            {
                node.Sftp.Connect();
                if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Reconnected SFTP to node {host}");
            }

            bool exists = node.Sftp.Exists(nodeFilePath);
            if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Checked existence of {host}:{nodeFilePath} → {(exists ? "exists" : "does not exist")}");
            return exists;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to check file existence on {host}:{nodeFilePath}: {ex.Message}");
            return false;
        }
    }
    public DateTime? NodeFileLastModified(string nodeFilePath, string host, bool verbose = false)
    {
        if (!_nodeConnections.TryGetValue(host, out var node))
            throw new InvalidOperationException($"Node {host} not initialized.");

        try
        {
            if (node.Sftp == null || !node.Sftp.IsConnected)
            {
                node.Sftp.Connect();
                if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Reconnected SFTP to node {host}");
            }

            var attrs = node.Sftp.GetAttributes(nodeFilePath);
            DateTime lastModified = attrs.LastWriteTime;

            if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Last modified {host}:{nodeFilePath} → {lastModified:yyyy-MM-dd HH:mm:ss}");
            return lastModified;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to get last modified time for {host}:{nodeFilePath}: {ex.Message}");
            return null;
        }
    }
    public bool DeleteNodeFile(string nodeFilePath, string host, bool verbose = false)
    {
        if (!_nodeConnections.TryGetValue(host, out var node))
            throw new InvalidOperationException($"Node {host} not initialized.");

        try
        {
            if (node.Sftp == null || !node.Sftp.IsConnected)
            {
                node.Sftp.Connect();
                if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Reconnected SFTP to node {host}");
            }

            if (node.Sftp.Exists(nodeFilePath))
            {
                node.Sftp.DeleteFile(nodeFilePath);
                if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Deleted {host}:{nodeFilePath}");
                return true;
            }
            else
            {
                if (verbose) logger.Log(LogLevel.WARN, "mCommunicator", $"File not found on {host}:{nodeFilePath} (nothing to delete)");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to delete file on {host}:{nodeFilePath}: {ex.Message}");
            return false;
        }
    }
    public bool MoveNodeFile(string currentFilePath, string newFilePath, string host, string username, bool verbose = false)
    {
        if (!_nodeConnections.TryGetValue(host, out var node))
            throw new InvalidOperationException($"Node {host} not initialized.");

        try
        {
            if (node.Sftp == null || !node.Sftp.IsConnected)
            {
                node.Sftp.Connect();
                if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Reconnected SFTP to node {host}");
            }

            if (!node.Sftp.Exists(currentFilePath))
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Move failed: source file not found on {host}:{currentFilePath}");
                return false;
            }

            node.Sftp.RenameFile(currentFilePath, newFilePath);

            if (node.Sftp.Exists(newFilePath))
            {
                if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Moved {host}:{currentFilePath} → {newFilePath}");
                return true;
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Move may have failed: destination not found {host}:{newFilePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to move file on {host}:{currentFilePath} → {newFilePath}: {ex.Message}");
            return false;
        }
    }


    //Command Methods
    public bool PingNode(string host, bool verbose = false)
    {
        try
        {
            if (!IsConnected)
            {
                Connect();
                if (!IsConnected)
                {
                    logger.Log(LogLevel.ERROR, "mCommunicator", "Failed to connect before checking node connection.");
                    return false;
                }
            }

            string cmd = $"ping -c 1 -W 2 {host} >/dev/null 2>&1 && echo connected || echo disconnected";
            var result = ExecuteHubCommand(cmd, verbose).Trim();

            bool isConnected = !(result.Contains("disconnected"));

            if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Node '{host}' connection check: {result}");
            
            return isConnected;
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Error checking connection for node '{host}': {ex.Message}");
            return false;
        }
    }
    public string ExecuteHubCommand(string command, bool verbose = false)
    {
        if (!IsConnected)
        {
            Connect();
            if (!IsConnected)
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "Failed to connect before executing command.");
                throw new InvalidOperationException("Not connected.");
            }
        }

        if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"SSH Executing> {command}");

        using (var cmd = _sshClient.CreateCommand(command))
        {
            var result = cmd.Execute();

            if (!string.IsNullOrEmpty(cmd.Error))
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"SSH Error: {cmd.Error}");
                throw new Exception($"SSH Error: {cmd.Error}");
            }

            if (verbose && !string.IsNullOrWhiteSpace(result))
                logger.Log(LogLevel.INFO, "mCommunicator", result);

            return result;
        }
    }
    public string ExecuteNodeCommand(string cmd, string host, string username, bool verbose = false)
    {
        if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Preparing to execute command on node {username}@{host}: {cmd}");

        // Escape quotes in command for safety
        string escapedCmd = cmd.Replace("\"", "\\\"");

        // Build SSH command that runs on the Hub to connect to the Node
        //string nodeCommand = $"ssh -tt {username}@{host} \"{cmd}\"";
        string nodeCommand = $"ssh -o BatchMode=yes {username}@{host} \"{escapedCmd}\"";
        if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"{username}: Executing via SSH-> {cmd}");
        return ExecuteHubCommand(nodeCommand, verbose);
    }

    //File Transfer Methods
    public bool CopyHubToNode(string hubFilePath, string nodeFilePath, string host, string username, bool verbose = false)
    {
        string cmd = $"scp \"{hubFilePath}\" {username}@{host}:\"{nodeFilePath}\"";
        ExecuteHubCommand(cmd, verbose);
        if (NodeFileExists(nodeFilePath, host, verbose))//, username, verbose))
        {
            if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Successfully copied {hubFilePath} to {username}@{host}:{nodeFilePath}");
            return true;
        }
        else
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Copy operation may have failed, remote file not found: {username}@{host}:{nodeFilePath}");
            return false;
        }
    }
    public bool CopyPCtoHub(string PCFilePath, string HubFilePath, bool verbose = false)
    {
        if (!IsConnected)
        {
            Connect();
            if (!IsConnected)
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "Failed to connect before executing node command.");
                throw new InvalidOperationException("Not connected to hub.");
            }
        }

        if (!File.Exists(PCFilePath))
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Local file not found: {PCFilePath}");
            return false;
        }

        try
        {
            using (var fs = new FileStream(PCFilePath, FileMode.Open))
            {
                using (var scp = new ScpClient(_host, _username, _password))
                {
                    scp.Connect();
                    if (scp.IsConnected)
                    {
                        scp.Upload(fs, HubFilePath);
                    }
                    else
                    {
                        logger.Log(LogLevel.ERROR, "mCommunicator", "SCP client failed to connect.");
                        throw new InvalidOperationException("SCP client not connected.");
                    }
                }
            }

            // Verify file presence on remote side
            if (HubFileExists(HubFilePath, verbose))
            {
                if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Successfully copied {PCFilePath} → {HubFilePath}");
                return true;
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Copy operation may have failed, remote file not found: {HubFilePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Error copying {PCFilePath} → {HubFilePath}: {ex.Message}");
            throw;
        }
    }
    public bool CopyHubToPC(string HubFilePath, string PCFilePath, bool verbose = false)
    {
        if (!IsConnected)
        {
            Connect();
            if (!IsConnected)
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "Failed to connect before executing node command.");
                throw new InvalidOperationException("Not connected to hub.");
            }
        }

        try
        {
            // Check remote file existence
            if (!HubFileExists(HubFilePath, verbose))
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Remote file not found: {HubFilePath}");
                return false;
            }

            // Copy file from remote
            using (var fs = new FileStream(PCFilePath, FileMode.Create))
            {
                using (var scp = new ScpClient(_host, _username, _password))
                {
                    scp.Connect();
                    if (scp.IsConnected)
                    {
                        scp.Download(HubFilePath, fs);
                    }
                    else
                    {
                        logger.Log(LogLevel.ERROR, "mCommunicator", "SCP client failed to connect.");
                        throw new InvalidOperationException("SCP client not connected.");
                    }
                    scp.Disconnect();
                }
            }

            // Verify local file exists and has content
            var localFileInfo = new FileInfo(PCFilePath);
            if (localFileInfo.Exists)
            {
                if (localFileInfo.Length > 0)
                {
                    if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Successfully copied {HubFilePath} → {PCFilePath} ({localFileInfo.Length} bytes)");
                    return true;
                }
                else
                {
                    logger.Log(LogLevel.ERROR, "mCommunicator", $"Copied file is empty: {PCFilePath}");
                    return false;
                }
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Copy failed: {HubFilePath} → {PCFilePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Error copying {HubFilePath} → {PCFilePath}: {ex.Message}");
            throw;
        }
    }
    public bool CopyNodeToPC(string nodeFilePath, string PCfilePath, string nodeName, bool verbose = false)
    {
        if (!_nodeConnections.TryGetValue(nodeName, out var node))
            throw new InvalidOperationException($"Node {nodeName} not initialized.");

        if (node.Sftp == null || !node.Sftp.IsConnected)
        {
            node.Sftp.Connect();
            if (verbose) logger.Log(LogLevel.DEBUG, "mCommunicator", $"Reconnected SFTP to node {nodeName}");
        }

        try
        {
            using (var fs = File.Create(PCfilePath))
            {
                node.Sftp.DownloadFile(nodeFilePath, fs);
            }
            if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Downloaded {nodeName}:{nodeFilePath} → {PCfilePath}");
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Failed to download file from {nodeName}:{nodeFilePath} → {PCfilePath}: {ex.Message}");
            throw;
        }

        var localFileInfo = new FileInfo(PCfilePath);
        if (localFileInfo.Exists)
        { 
            if (localFileInfo.Length > 0)
            {
                if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Successfully downloaded {nodeName}:{nodeFilePath} → {PCfilePath}");
                return true;
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", $"Download resulted in empty file: {PCfilePath}");
                return false;
            }
        }
        else
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Download failed, local file not found: {PCfilePath}");
            return false;
        }
        //if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Downloaded {nodeName}:{nodeFilePath} → {PCfilePath}");
    }
    public bool CopyPCtoNode(string PCfilePath, string nodeFilePath, string host, bool verbose = false)
    {
        if (!IsConnected)
        {
            Connect();
            if (!IsConnected)
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "Failed to connect before executing command.");
                throw new InvalidOperationException("Not connected.");
            }
        }

        if (!_nodeConnections.TryGetValue(host, out var node))
            throw new InvalidOperationException($"Node {host} not initialized.");

        using (var fs = File.OpenRead(PCfilePath))
        {
            node.Sftp.UploadFile(fs, nodeFilePath, true);
        }
        if (NodeFileExists(nodeFilePath, host, verbose))//, node.Username, verbose))
        {
            if (verbose) logger.Log(LogLevel.INFO, "mCommunicator", $"Successfully uploaded {PCfilePath} → {host}:{nodeFilePath}");
            return true;
        }
        else
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Upload may have failed, remote file not found: {host}:{nodeFilePath}");
            return false;
        }
    }

    //testMethod
    public void testFileMethods(string PCfilePath, string host, string username)
    {
        try
        {
            string hubFilePath = $"/tmp/{Path.GetFileName(PCfilePath)}";
            string nodeHubFilePath = $"/tmp/fromHub{Path.GetFileName(PCfilePath)}";
            string pcHubCopyBackPath = PCfilePath.Substring(0, PCfilePath.Length - 4) + "_HubCopyback.txt";

            string nodePCFilePath = $"/tmp/fromPC{Path.GetFileName(PCfilePath)}";
            string pcNodeCopyBackPath = PCfilePath.Substring(0, PCfilePath.Length - 4) + "_Nodecopyback.txt";

            if (HubFileExists(hubFilePath))
            {
                logger.Log(LogLevel.DEBUG, "mCommunicator", $"Deleting hub file {hubFilePath} before test.");
                DeleteHubFile(hubFilePath);
            }
            if (NodeFileExists(nodeHubFilePath, host))//, username))
            {
                logger.Log(LogLevel.DEBUG, "mCommunicator", $"Deleting node file {nodeHubFilePath} before test.");
                DeleteNodeFile(nodeHubFilePath, host);//, username);
            }
            if (NodeFileExists(nodePCFilePath, host))//, username))
            {
                logger.Log(LogLevel.DEBUG, "mCommunicator", $"Deleting node file {nodePCFilePath} before test.");
                DeleteNodeFile(nodePCFilePath, host);//, username);
            }
            if (File.Exists(pcHubCopyBackPath))
            {
                logger.Log(LogLevel.DEBUG, "mCommunicator", $"Deleting PC file {pcHubCopyBackPath} before test.");
                File.Delete(pcHubCopyBackPath);
            }
            if (File.Exists(pcNodeCopyBackPath))
            {
                logger.Log(LogLevel.DEBUG, "mCommunicator", $"Deleting PC file {pcNodeCopyBackPath} before test.");
                File.Delete(pcNodeCopyBackPath);
            }
            // Copy PC to Hub
            logger.Log(LogLevel.DEBUG, "mCommunicator", "Starting test:");
            logger.Log(LogLevel.DEBUG, "mCommunicator", "   Copy PC to Hub");
            CopyPCtoHub(PCfilePath, hubFilePath);
            if (HubFileExists(hubFilePath))
            {
                logger.Log(LogLevel.INFO, "mCommunicator", "       Success");
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "       Fail");
                return;
            }

            // Copy Hub to Node
            logger.Log(LogLevel.DEBUG, "mCommunicator", "   Copy Hub to Node");
            CopyHubToNode(hubFilePath, nodeHubFilePath, host, username);
            if (NodeFileExists(nodeHubFilePath, host))//, username))
            {
                logger.Log(LogLevel.INFO, "mCommunicator", "       Success");
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "       Fail");
                return;
            }
            // Copy Hub to PC
            logger.Log(LogLevel.DEBUG, "mCommunicator", "   Copy Hub to PC");
            CopyHubToPC(hubFilePath, pcHubCopyBackPath);
            if (File.Exists(pcHubCopyBackPath))
            {
                logger.Log(LogLevel.INFO, "mCommunicator", "       Success");
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "       Fail");
                return;
            }

            // Copy PC to Node (to a different temp name)
            logger.Log(LogLevel.DEBUG, "mCommunicator", "   Copy PC to Node");
            CopyPCtoNode(PCfilePath, nodePCFilePath, host);
            if (NodeFileExists(nodePCFilePath, host))//, username))
            {
                logger.Log(LogLevel.INFO, "mCommunicator", "       Success");
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "       Fail");
            }

            // Copy Node back to PC (different name)
            logger.Log(LogLevel.DEBUG, "mCommunicator", "   Copy Node back to PC");
            CopyNodeToPC(nodePCFilePath, pcNodeCopyBackPath, host);
            if (File.Exists(pcNodeCopyBackPath))
            {
                logger.Log(LogLevel.INFO, "mCommunicator", "       Success");
            }
            else
            {
                logger.Log(LogLevel.ERROR, "mCommunicator", "       Fail");
            }

            //DeleteHubFile(hubFilePath);
            //DeleteHubFile(hubCopyBackPath);
            //DeleteNodeFile(nodeFilePath, host, username);
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.ERROR, "mCommunicator", $"Test encountered error: {ex.Message}");
        }
    }
}
