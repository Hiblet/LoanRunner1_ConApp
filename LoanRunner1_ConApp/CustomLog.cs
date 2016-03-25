using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections.Concurrent;
using System.Threading;
using System.Configuration; // Configuration Manager
using log4net.Core; // Level, log4Net log level

namespace NZ01
{

    public class CustomLog
    {
        // Static Members
        private static log4net.ILog _logger = log4net.LogManager.GetLogger("CustomLog");
       
        private static ConcurrentQueue<Tuple<int ,string, Exception>> _queue = new ConcurrentQueue<Tuple<int, string, Exception>>();

        private static Thread _thread;
        private static bool _running = true;
        private static AutoResetEvent _eventThreadExit = new AutoResetEvent(false);
        private static AutoResetEvent _eventThreadAction = new AutoResetEvent(false);
        private static int _waitTimeout = 1000; // 1 second - Effects responsiveness to shutdown

        private static string _qSizeFormatter = "D8";
        private static string _dateTimeFormatter = "HH:mm:ss.fff";

        private static int _thresholdLevelWarn = 10000; // Warn but accept messages if this number of messages is on the queue
        private static bool _bLevelWarnPassed = false;
        private static int _thresholdLevelError = 1000000; // Error and reject messages if this number of messages is on the queue
        private static bool _bLevelErrorPassed = false;


        // Instance Members
        private string _name = "NAME_NOT_DEFINED";
        private bool _bConsoleWrite = false;


        // Enum
        public enum CustomLogLevel { UNKNOWN = -1, UNUSED0 = 0, UNUSED1 = 1, FATAL = 2, ERROR = 3, WARN = 4, INFO = 5, DEBUG = 6, MAX = 7 };



        // Static Ctor
        static CustomLog()
        {
            var prefix = "CustomLog() [STATIC CTOR] - ";

            // Start Log4Net
            if (!log4net.LogManager.GetRepository().Configured)
            {
                // Set up Log4net
                log4net.Config.XmlConfigurator.Configure();
            }

            // Pull in Config values if they exist
            loadIntVariable(ref _thresholdLevelWarn, "CustomLogQMessageThresholdLevelWarn");
            loadIntVariable(ref _thresholdLevelError, "CustomLogQMessageThresholdLevelError");
            setQSizeFormatter();

            _thread = new Thread(new ThreadStart(RunThread));
            _thread.Name = "CUSTOMLOG";

            _logger.Info(prefix + "About to start CustomLog Thread...");
            _thread.Start(); 
            _logger.Info(prefix + "CustomLog Thread started...");
        }



        //////////////////
        // Instance Ctor

        public CustomLog(string name, bool bConsoleWrite = false)
        {
            _name = name;
            _bConsoleWrite = bConsoleWrite;
        }




        /////////////////////
        // Member Functions

        public static void Stop()
        {
            var prefix = "Stop() - ";
            
            _running = false;

            if (_eventThreadExit.WaitOne())            
                _logger.Info(prefix + "Graceful Shutdown - CustomLog Thread has signalled that it has stopped.");            
            else            
                _logger.Info(prefix + "Bad Shutdown - CustomLog Thread did not signal that it had stopped.");            
        }



        /////////////////
        // Log Wrappers        

        public void Log(Level level, object message, Exception ex = null)
        {
            if (level == Level.Debug)
                Debug(message, ex);
            else if (level == Level.Info)
                Info(message, ex);
            else if (level == Level.Warn)
                Warn(message, ex);
            else if (level == Level.Error)
                Error(message, ex);
            else if (level == Level.Fatal)
                Fatal(message, ex);
            else
                Info(message, ex);
        }

        public void Debug(object message, Exception ex = null)
        {
            enqueueAndSignal((int)CustomLog.CustomLogLevel.DEBUG, message.ToString(), ex);
        }

        public void Info(object message, Exception ex = null)
        {
            enqueueAndSignal((int)CustomLog.CustomLogLevel.INFO, message.ToString(), ex);
        }

        public void Warn(object message, Exception ex = null)
        {
            enqueueAndSignal((int)CustomLog.CustomLogLevel.WARN, message.ToString(), ex);
        }

        public void Error(object message, Exception ex = null)
        {
            enqueueAndSignal((int)CustomLog.CustomLogLevel.ERROR, message.ToString(), ex);
        }

        public void Fatal(object message, Exception ex = null)
        {
            enqueueAndSignal((int)CustomLog.CustomLogLevel.FATAL, message.ToString(), ex);
        }

        
        
        private void enqueueAndSignal(int level, string message, Exception ex)
        {
            int countQueued = _queue.Count;
            if (checkCapacity(countQueued))
            {
                ///////////////////////
                // Enqueue and Signal

                string sThreadID = string.Format("NQTHR={0},",Thread.CurrentThread.ManagedThreadId.ToString("D3"));

                string sCount = string.Format("NQ={0},", countQueued.ToString(_qSizeFormatter));
                
                string sTime = string.Format("NQUTC={0},", DateTime.UtcNow.ToString(_dateTimeFormatter));
                
                string sThreadName = (Thread.CurrentThread.Name == null) ? "" : string.Format("THRNM={0},", Thread.CurrentThread.Name);   
                
                string s = sCount + sTime + sThreadID + sThreadName + _name + "," + message;
                
                Tuple<int, string, Exception> tuple = new Tuple<int, string, Exception>(level, s, ex);
                
                _queue.Enqueue(tuple);
                _eventThreadAction.Set();
            }

            if (_bConsoleWrite)
            {
                Console.WriteLine(message);
            }
        }


        private static bool checkCapacity(int countQueued)
        {
            var prefix = "checkCapacity() - ";

            if (countQueued > _thresholdLevelWarn && !_bLevelWarnPassed)
            {
                _bLevelWarnPassed = true;
                string msg = string.Format("The logging message queue has passed {0} messages.", _thresholdLevelWarn);
                _logger.Warn(prefix + msg);
            }

            if (countQueued > _thresholdLevelError)
            {
                if (!_bLevelErrorPassed)
                {
                    _bLevelErrorPassed = true;
                    string msg = string.Format("The logging message queue has passed {0} messages.", _thresholdLevelError);
                    _logger.Error(prefix + msg);
                }

                return false; // Do not add this message to queue
            }

            return true; // Queue message
        }
        


        //////////////////
        // Get/Set Flags

        public static bool WarnFlag(bool value)
        { 
            _bLevelWarnPassed = value;
            return _bLevelWarnPassed;
        }

        public static bool ErrorFlag(bool value)
        { 
            _bLevelErrorPassed = value;
            return _bLevelErrorPassed;
        }



        ///////////////////////////
        // Get/Set Warning Levels

        public static int ThresholdLevelWarn(int qSize)
        { 
            _thresholdLevelWarn = qSize;
            return _thresholdLevelWarn;
        }

        public static int ThresholdLevelWarn()
        { return _thresholdLevelWarn; }

        public static int ThresholdLevelError(int qSize)
        { 
            _thresholdLevelError = qSize;
            setQSizeFormatter();
            return _thresholdLevelError;
        }

        public static int ThresholdLevelError()
        { return _thresholdLevelError; }

        private static void setQSizeFormatter()
        {
            double dLogTen = Math.Log10((double)_thresholdLevelError);
            int iLogTen = (int)Math.Ceiling(dLogTen);
            if (iLogTen <= 0)
                _qSizeFormatter = "D4";
            else
                _qSizeFormatter = "D" + iLogTen.ToString(); // eg "D8"
        }


        
        ////////////
        // Helpers

        /// <summary>
        /// Load an int value from config
        /// </summary>
        /// <param name="iValue">Reference to int value to receive</param>
        /// <param name="sKey">string; Entry in the config</param>
        private static void loadIntVariable(ref int iValue, string sKey)
        {
            string sCandidate = ConfigurationManager.AppSettings[sKey];
            int iCandidate;
            if (Int32.TryParse(sCandidate, out iCandidate))
            {
                iValue = iCandidate;
            }
        }


        
        ///////////////////////
        // Internal Mechanics

        private static void consumeQueue()
        {
            Tuple<int, string, Exception> tuple;
            while (_queue.TryDequeue(out tuple))
            {
                processQueuedItem(tuple, _queue.Count);
            }
        }

        private static void processQueuedItem(Tuple<int, string, Exception> tuple, int countQueued)
        {
            string sCountWhenDequeued = string.Format("DQ={0},", countQueued.ToString(_qSizeFormatter));
            string msg = sCountWhenDequeued + tuple.Item2;

            switch (tuple.Item1)
            {
                case (int)CustomLog.CustomLogLevel.DEBUG:
                    if (tuple.Item3 == null)
                        _logger.Debug(msg);
                    else
                        _logger.Debug(msg, tuple.Item3);
                    return;
                case (int)CustomLog.CustomLogLevel.WARN:
                    if (tuple.Item3 == null)
                        _logger.Warn(msg);
                    else
                        _logger.Warn(msg, tuple.Item3);
                    return;
                case (int)CustomLog.CustomLogLevel.ERROR:
                    if (tuple.Item3 == null)
                        _logger.Error(msg);
                    else
                        _logger.Error(msg, tuple.Item3);
                    return;
                case (int)CustomLog.CustomLogLevel.FATAL:
                    if (tuple.Item3 == null)
                        _logger.Fatal(msg);
                    else
                        _logger.Fatal(msg, tuple.Item3);
                    return;
                default:
                case (int)CustomLog.CustomLogLevel.INFO:
                    if (tuple.Item3 == null)
                        _logger.Info(msg);
                    else
                        _logger.Info(msg, tuple.Item3);
                    return;
            }
        }


        ////////////////////
        // Thread function

        private static void RunThread()
        {
            while (_running)
            {
                if (_eventThreadAction.WaitOne(_waitTimeout))                
                    consumeQueue(); // Signal Received                
            }

            // Signal that the thread has exited.
            _eventThreadExit.Set();
        }



    } // end of class CustomLog

} // end of namespace NZ01
