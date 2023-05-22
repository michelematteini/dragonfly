using System;
using System.IO;
using System.Threading.Tasks;

namespace Dragonfly.Utils
{
    /// <summary>
    /// Load files asyncronously, and execute the specified callbacks when completed.
    /// </summary>
    public class AsyncFileLoader
    {
        public static void LoadAllFiles(string[] files, ILoadedFileHandler resultHandler, bool loadAsBinary)
        {
            new AsyncLoadAndCloseAdapter(files, resultHandler, loadAsBinary).StartLoading();
        }

        public static void LoadFile(string filePath, ILoadedFileHandler resultHandler, bool loadAsBinary)
        {
            new AsyncLoadAndCloseAdapter(new string[] { filePath }, resultHandler, loadAsBinary).StartLoading();
        }

        Task loadingLoop;

        private struct FileRequest
        {
            public int RequestID;
            public string FilePath;
            public ILoadedFileHandler ResultHandler;
            public byte[] LoadedFile;
            public string LoadedText;
            public bool IsBinary;
        }

        private BlockingQueue<FileRequest> requests;
        private bool stopRequest;


        public AsyncFileLoader()
        {
            requests = new BlockingQueue<FileRequest>();
            Idle = true;
        }

        public void RequestFile(int requestID, string filePath, ILoadedFileHandler resultHandler, bool isBinary)
        {
            FileRequest request = new FileRequest();
            request.RequestID = requestID;
            request.FilePath = filePath;
            request.ResultHandler = resultHandler;
            request.IsBinary = isBinary;
            requests.Enqueue(request);
        }

        public void Start()
        {
            if (IsRunning) return;
            stopRequest = false;
            IsRunning = true;
            TaskFactory tasks = new TaskFactory();
            loadingLoop = tasks.StartNew(new Action(FileLoadingLoop), TaskCreationOptions.LongRunning);
        }

        public bool IsRunning
        {
            get; private set;
        }

        public bool Idle
        {
            get; private set;
        }

        public void Stop(bool waitCompletion = false)
        {
            stopRequest = true;
            if (waitCompletion) loadingLoop.Wait();
        }
       
        private void FileLoadingLoop()
        {
            while (!stopRequest)
            {
                FileRequest request;
                if (!requests.TryDequeue(out request, 1000)) continue;

                Idle = false;

                if (request.IsBinary)
                {
                    request.LoadedFile = File.ReadAllBytes(request.FilePath);
                    request.ResultHandler.OnFileLoaded(request.RequestID, request.FilePath, request.LoadedFile);
                }
                else
                {
                    request.LoadedText = File.ReadAllText(request.FilePath);
                    request.ResultHandler.OnFileLoaded(request.RequestID, request.FilePath, request.LoadedText);
                }

                Idle = requests.Count == 0;
            }

            IsRunning = false;
        }

    }

    /// <summary>
    /// Load a group of files asynchronously using an AsyncFileLoader
    /// </summary>
    internal class AsyncLoadAndCloseAdapter : ILoadedFileHandler
    {
        int leftCount;
        ILoadedFileHandler resultHandler;
        AsyncFileLoader fileLoader;

        public AsyncLoadAndCloseAdapter(string[] files, ILoadedFileHandler resultHandler, bool loadAsBinary)
        {
            leftCount = files.Length;
            this.resultHandler = resultHandler;
            fileLoader = new AsyncFileLoader();
            foreach (string filePath in files)
                fileLoader.RequestFile(0, filePath, this, loadAsBinary);
        }

        public void StartLoading()
        {
            fileLoader.Start();
        }

        public void OnFileLoaded(int requestID, string filePath, byte[] loadedBytes)
        {
            leftCount--;
            resultHandler.OnFileLoaded(requestID, filePath, loadedBytes);

            if (leftCount == 0)
                fileLoader.Stop();
        }

        public void OnFileLoaded(int requestID, string filePath, string loadedText)
        {
            leftCount--;
            resultHandler.OnFileLoaded(requestID, filePath, loadedText);

            if (leftCount == 0)
                fileLoader.Stop();
        }
    }

    public interface ILoadedFileHandler
    {
        void OnFileLoaded(int requestID, string filePath, byte[] loadedBytes);

        void OnFileLoaded(int requestID, string filePath, string loadedText);
    }

    public class TextFileHandler : ILoadedFileHandler
    {
        private Action<string> callBack;
        public TextFileHandler(Action<string> callBack)
        {
            this.callBack = callBack;
        }

        public void OnFileLoaded(int requestID, string filePath, byte[] loadedBytes) { }

        public void OnFileLoaded(int requestID, string filePath, string loadedText)
        {
            callBack(loadedText);
        }
    }

    public class BinaryFileHandler : ILoadedFileHandler
    {
        private Action<byte[]> callBack;

        public BinaryFileHandler(Action<byte[]> callBack)
        {
            this.callBack = callBack;
        }

        public void OnFileLoaded(int requestID, string filePath, byte[] loadedBytes)
        {
            callBack(loadedBytes);
        }

        public void OnFileLoaded(int requestID, string filePath, string loadedText) { }
    }

}
