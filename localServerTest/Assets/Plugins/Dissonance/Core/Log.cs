using System;
using System.Diagnostics;
using System.Threading;
using Dissonance.Config;
using Dissonance.Datastructures;

namespace Dissonance
{
    public enum LogLevel
    {
        /// <summary>
        /// Per-frame diagnostic events.
        /// </summary>
        Trace = 0,

        /// <summary>
        /// Significant diagnostic events.
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Significant events which occur under normal operation.
        /// </summary>
        Info = 2,

        /// <summary>
        /// Non-critical errors, which deserve investigation.
        /// </summary>
        Warn = 3,

        /// <summary>
        /// Critical errors caused by external factors, outside of missuse or bugs.
        /// </summary>
        Error = 4
    }

    public enum LogCategory
    {
        Core,
        Recording,
        Network,
        Playback
    }

    public static class Logs
    {
        [NotNull] public static Log Create(LogCategory category, string name)
        {
            return Create((int)category, name);
        }

        [NotNull] public static Log Create(int category, string name)
        {
            return new Log(category, name);
        }

        public static void SetLogLevel(LogCategory category, LogLevel level)
        {
            SetLogLevel((int)category, level);
        }

        public static void SetLogLevel(int category, LogLevel level)
        {
            DebugSettings.Instance.SetLevel(category, level);
        }

        public static LogLevel GetLogLevel(LogCategory category)
        {
            return GetLogLevel((int)category);
        }

        public static LogLevel GetLogLevel(int category)
        {
            return DebugSettings.Instance.GetLevel(category);
        }

        #region multithreading
        private struct LogMessage
        {
            private readonly LogLevel _level;
            private readonly string _message;

            public LogMessage(string message, LogLevel level)
            {
                _message = message;
                _level = level;
            }

            public void Log()
            {
                switch (_level)
                {
                    case LogLevel.Trace:
                        UnityEngine.Debug.Log(_message);
                        break;
                    case LogLevel.Debug:
                        UnityEngine.Debug.Log(_message);
                        break;
                    case LogLevel.Info:
                        UnityEngine.Debug.Log(_message);
                        break;
                    case LogLevel.Warn:
                        UnityEngine.Debug.LogWarning(_message);
                        break;
                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(_message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static readonly TransferBuffer<LogMessage> LogsFromOtherThreads = new TransferBuffer<LogMessage>(512);
        private static Thread _main;

        internal static void WriteMultithreadedLogs()
        {
            if (_main == null)
                _main = Thread.CurrentThread;

            LogMessage msg;
            while (LogsFromOtherThreads.Read(out msg))
                msg.Log();
        }

        internal static void SendLogMessage(string message, LogLevel level)
        {
#if NCRUNCH
            Console.WriteLine(message);
#else
            var msg = new LogMessage(message, level);

            if (_main == Thread.CurrentThread)
                msg.Log();
            else
                LogsFromOtherThreads.TryWrite(msg);
#endif
        }
        #endregion
    }

    public class Log
    {
        private readonly string _format;
        private readonly int _category;

        internal Log(int category, string name)
        {
            _category = category;
            _format = "[Dissonance:" + (LogCategory) category + "] " + name + ": {0}";
        }

        public bool IsTrace
        {
            get { return ShouldLog(LogLevel.Trace); }
        }

        public bool IsDebug
        {
            get { return ShouldLog(LogLevel.Debug); }
        }

        public bool IsInfo
        {
            get { return ShouldLog(LogLevel.Info); }
        }

        public bool IsWarn
        {
            get { return ShouldLog(LogLevel.Warn); }
        }

        public bool IsError
        {
            get { return ShouldLog(LogLevel.Error); }
        }

        [DebuggerHidden]
        private bool ShouldLog(LogLevel level)
        {
            return level >= Logs.GetLogLevel(_category);
        }

        #region Logging implementation
        [DebuggerHidden]
        private void WriteLog(LogLevel level, string message)
        {
            if (!ShouldLog(level))
                return;

            var msg = string.Format(_format, message);

            switch (level)
            {
                case LogLevel.Trace:
                    Logs.SendLogMessage(string.Format("TRACE {0}", msg), level);
                    break;

                case LogLevel.Debug:
                    Logs.SendLogMessage(string.Format("DEBUG {0}", msg), level);
                    break;

                case LogLevel.Info:
                case LogLevel.Warn:
                case LogLevel.Error:
                    Logs.SendLogMessage(msg, level);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("level", level, null);
            }
        }

        [DebuggerHidden]
        private void WriteLogFormat(LogLevel level, string format, params object[] parameters)
        {
            if (!ShouldLog(level))
                return;

            WriteLog(level, string.Format(format, parameters));
        }

        [DebuggerHidden]
        private void WriteLogFormat<TA>(LogLevel level, string format, [CanBeNull] TA p0)
        {
            if (!ShouldLog(level))
                return;

            WriteLog(level, string.Format(format, p0));
        }

        [DebuggerHidden]
        private void WriteLogFormat<TA, TB>(LogLevel level, string format, [CanBeNull] TA p0, [CanBeNull] TB p1)
        {
            if (!ShouldLog(level))
                return;

            WriteLog(level, string.Format(format, p0, p1));
        }

        [DebuggerHidden]
        private void WriteLogFormat<TA, TB, TC>(LogLevel level, string format, [CanBeNull] TA p0, [CanBeNull] TB p1, [CanBeNull] TC p2)
        {
            if (!ShouldLog(level))
                return;

            WriteLog(level, string.Format(format, p0, p1, p2));
        }
#endregion

        #region Trace
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Trace(string message)
        {
            WriteLog(LogLevel.Trace, message);
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Trace<TA>(string format, [CanBeNull] TA p0)
        {
            WriteLogFormat(LogLevel.Trace, format, p0);
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Trace<TA, TB>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1)
        {
            WriteLogFormat(LogLevel.Trace, format, p0, p1);
        }
#endregion

        #region Debug
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Debug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Debug<TA>(string format, [CanBeNull] TA p0)
        {
            WriteLogFormat(LogLevel.Debug, format, p0);
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Debug<TA, TB>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1)
        {
            WriteLogFormat(LogLevel.Debug, format, p0, p1);
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Debug<TA, TB, TC>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1, [CanBeNull] TC p2)
        {
            WriteLogFormat(LogLevel.Debug, format, p0, p1, p2);
        }

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public void Debug<TA, TB, TC, TD>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1, [CanBeNull] TC p2, [CanBeNull] TD p3)
        {
            WriteLogFormat(LogLevel.Debug, format, p0, p1, p2, p3);
        }
        #endregion

        #region info
        [DebuggerHidden]
        public void Info(string message)
        {
            WriteLog(LogLevel.Info, message);
        }

        [DebuggerHidden]
        public void Info<TA>(string format, [CanBeNull] TA p0)
        {
            WriteLogFormat(LogLevel.Info, format, p0);
        }

        [DebuggerHidden]
        public void Info<TA, TB>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1)
        {
            WriteLogFormat(LogLevel.Info, format, p0, p1);
        }

        [DebuggerHidden]
        public void Info<TA, TB, TC>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1, [CanBeNull] TC p2)
        {
            WriteLogFormat(LogLevel.Info, format, p0, p1, p2);
        }
        #endregion

        #region warn
        [DebuggerHidden]
        public void Warn(string message)
        {
            WriteLog(LogLevel.Warn, message);
        }

        [DebuggerHidden]
        public void Warn<TA>(string format, [CanBeNull] TA p0)
        {
            WriteLogFormat(LogLevel.Warn, format, p0);
        }

        [DebuggerHidden]
        public void Warn<TA, TB>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1)
        {
            WriteLogFormat(LogLevel.Warn, format, p0, p1);
        }

        [DebuggerHidden]
        public void Warn<TA, TB, TC>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1, [CanBeNull] TC p2)
        {
            WriteLogFormat(LogLevel.Warn, format, p0, p1, p2);
        }
        #endregion

        #region error
        [DebuggerHidden]
        public void Error(string message)
        {
            WriteLog(LogLevel.Error, message);
        }

        [DebuggerHidden]
        public void Error<TA>(string format, [CanBeNull] TA p0)
        {
            WriteLogFormat(LogLevel.Error, format, p0);
        }

        [DebuggerHidden]
        public void Error<TA, TB>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1)
        {
            WriteLogFormat(LogLevel.Error, format, p0, p1);
        }

        [DebuggerHidden]
        public void Error<TA, TB, TC>(string format, [CanBeNull] TA p0, [CanBeNull] TB p1, [CanBeNull] TC p2)
        {
            WriteLogFormat(LogLevel.Error, format, p0, p1, p2);
        }
#endregion

        #region throw
        [DebuggerHidden, NotNull]
        public DissonanceException CreateUserErrorException(string problem, string likelyCause, string documentationLink, string guid)
        {
            return new DissonanceException(UserErrorMessage(problem, likelyCause, documentationLink, guid));
        }

        [DebuggerHidden, NotNull]
        public string UserErrorMessage(string problem, string likelyCause, string documentationLink, string guid)
        {
            var message = string.Format(
                "Error: {0}! This is likely caused by \"{1}\", see the documentation at \"{2}\" or visit the community at \"http://placeholder-software.co.uk/dissonance/community\" to get help. Error ID: {3}",
                problem,
                likelyCause,
                documentationLink,
                guid
            );

            return string.Format(_format, message);
        }

        [DebuggerHidden, NotNull]
        public string PossibleBugMessage(string problem, string guid)
        {
            return string.Format(
                "Error: {0}! This is probably a bug in Dissonance, we're sorry! Please report the bug on the issue tracker \"https://github.com/Placeholder-Software/Dissonance/issues\". You could also seek help on the community at \"http://placeholder-software.co.uk/dissonance/community\" to get help for a temporary workaround. Error ID: {1}",
                problem,
                guid
            );
        }

        [DebuggerHidden, NotNull]
        public DissonanceException CreatePossibleBugException(string problem, string guid)
        {
            return new DissonanceException(PossibleBugMessage(problem, guid));
        }

        [DebuggerHidden, NotNull]
        public Exception CreatePossibleBugException<T>([NotNull] Func<string, T> factory, string problem, string guid) where T : Exception
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            return factory(PossibleBugMessage(problem, guid));
        }
        #endregion

        #region checks
        /// <summary>
        /// Check an assertion and return if the assertion failed. Log a warn message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:true => false; assertion:false => true")]
        public bool AssertAndLogWarn(bool assertion, string msg)
        {
            if (!assertion)
                Warn(msg);

            return !assertion;
        }

        /// <summary>
        /// Check an assertion and return if the assertion failed. Log a warn message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:true => false; assertion:false => true")]
        public bool AssertAndLogWarn<TA>(bool assertion, string format, TA arg0)
        {
            if (!assertion)
                Warn(format, arg0);

            return !assertion;
        }

        /// <summary>
        /// Check an assertion and return if the assertion failed. Log an error (probablebug) message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="guid"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:true => false; assertion:false => true")]
        public bool AssertAndLogError(bool assertion, string guid, string msg)
        {
            if (!assertion)
                Error(PossibleBugMessage(msg, guid));

            return !assertion;
        }

        /// <summary>
        /// Check an assertion and return if the assertion failed. Log an error (probablebug) message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="guid"></param>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:true => false; assertion:false => true")]
        public bool AssertAndLogError<TA>(bool assertion, string guid, string format, TA arg0)
        {
            if (!assertion)
                Error(PossibleBugMessage(string.Format(format, arg0), guid));

            return !assertion;
        }

        /// <summary>
        /// Check an assertion and return if the assertion failed. Log an error (probablebug) message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="guid"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:false => halt")]
        public void AssertAndThrowPossibleBug(bool assertion, string guid, string msg)
        {
            if (!assertion)
                throw CreatePossibleBugException(msg, guid);
        }

        /// <summary>
        /// Check an assertion and return if the assertion failed. Log an error (probablebug) message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="guid"></param>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:false => halt")]
        public void AssertAndThrowPossibleBug<TA>(bool assertion, string guid, string format, TA arg0)
        {
            if (!assertion)
                throw CreatePossibleBugException(string.Format(format, arg0), guid);
        }

        /// <summary>
        /// Check an assertion and return if the assertion failed. Log an error (probablebug) message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="guid"></param>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:false => halt")]
        public void AssertAndThrowPossibleBug<TA, TB>(bool assertion, string guid, string format, TA arg0, TB arg1)
        {
            if (!assertion)
                throw CreatePossibleBugException(string.Format(format, arg0, arg1), guid);
        }

        /// <summary>
        /// Check an assertion and return if the assertion failed. Log an error (probablebug) message if it fails
        /// </summary>
        /// <param name="assertion"></param>
        /// <param name="guid"></param>
        /// <param name="format"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        [ContractAnnotation("assertion:false => halt")]
        public void AssertAndThrowPossibleBug<TA, TB, TC>(bool assertion, string guid, string format, TA arg0, TB arg1, TC arg2)
        {
            if (!assertion)
                throw CreatePossibleBugException(string.Format(format, arg0, arg1, arg2), guid);
        }
        #endregion
    }
}
