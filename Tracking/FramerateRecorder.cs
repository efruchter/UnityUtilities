using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Kazoo.Diagnostics
{
    /// <summary>
    /// Track where in a session the framerate drops below a reasonable rate, and
    /// output the data to a file that excel can plot. Provides methods for pardoning intensive frames.
    /// 
    /// Start a session with `FramerateRecorder.BeginSession(config);`
    /// Update a session with `FramerateRecorder.SubmitFrame();`
    /// End a session with `FramerateRecorder.EndAndSaveSession("Comment", "frames.tsv");`
    ///     or one of itss variants.
    /// Prior to doing intensive work, call `FramerateRecorder.PardonFrame();`
    /// 
    /// -Eric
    /// </summary>
    public static class FramerateRecorder
    {
        public static bool AllowRecording
        {
            get { return true; }
        }

        private static SessionRecord _currentSession;

        /// <summary>
        /// Begin a framerate tracking session.
        /// </summary>
        public static void BeginSession(SessionConfugration config)
        {
            if (!AllowRecording) return;

            if (_currentSession != null)
            {
                Debug.LogError("FramerateRecord: Starting a session when another one is already active.");
                return;
            }

            _currentSession = new SessionRecord(config);
            _currentSession.BeginRecording();
            PardonFrame();
        }

        /// <summary>
        /// Pardon this particular frame, if you know you are going to miss framerate.
        /// </summary>
        public static void PardonFrame()
        {
            if (!AllowRecording) return;

            if (_currentSession == null)
            {
                Debug.LogError("FramerateRecord: Cannot pardon a frame unless a session has begun.");
                return;
            }

            _currentSession.PardonCurrentFrame();
        }

        /// <summary>
        /// Submit the current frame for record-keeping.
        /// </summary>
        public static void SubmitFrame()
        {
            if (!AllowRecording) return;

            if (_currentSession == null)
            {
                Debug.LogError("FramerateRecord: Cannot submit a frame unless a session has begun.");
                return;
            }

            _currentSession.SubmitCurrentFrame();
        }

        /// <summary>
        /// End the session and wipe the data.
        /// </summary>
        public static void EndSession()
        {
            if (!AllowRecording) return;

            _currentSession = null;
        }

        /// <summary>
        /// End the session, wipe the data, and return a data sheet in TSV format.
        /// </summary>
        /// <returns>Tab Seperated Value Spreadsheet string</returns>
        public static string EndAndPrintSession()
        {
            if (!AllowRecording) return string.Empty;

            if (_currentSession == null)
            {
                Debug.LogError("FramerateRecord: Trying to end a session that has not been started.");
                return string.Empty;
            }

            string data = _currentSession.ProduceTsvFromRecord();

            EndSession();

            return data;
        }

        /// <summary>
        /// End the session, wipe the data, and return a data sheet in TSV format, as well as write it to a file.
        /// </summary>
        /// <param name="comment"></param>
        /// <param name="pathToOutput"></param>
        public static string EndAndSaveSession(string comment, string pathToOutput)
        {
            if (!AllowRecording) return string.Empty;

            if (_currentSession == null)
            {
                Debug.LogError("FramerateRecord: Trying to end a session that has not been started.");
                return string.Empty;
            }

            try
            {
                string output = EndAndPrintSession();
                string content = comment + "\n" + output;
                File.WriteAllText(pathToOutput, content);
                Debug.Log("FPS Record written to file: \"" + pathToOutput + "\"");
                return content;
            }
            catch (Exception e)
            {
                Debug.LogError("There was an error writing the fps log:" + e.Message);

                if (_currentSession != null)
                {
                    EndSession();
                }

                return string.Empty;
            }
        }

        private sealed class SessionRecord
        {
            private readonly SessionConfugration _configuration;
            private readonly LinkedList<FrameRecord> _alertRecord = new LinkedList<FrameRecord>();

            private float _lastFrameTime = -1f;
            private float _sessionStartingTime = -1;

            private bool _ongoingRegion;
            private FrameRecord _ongoingRecord;
            private bool _pardonActive;
            private int _sampleCount;
            private float _averageFps;

            public SessionRecord(SessionConfugration config)
            {
                _configuration = config;
            }

            public void BeginRecording()
            {
                _lastFrameTime = 0;
                _sessionStartingTime = _lastFrameTime;
                _ongoingRegion = false;
                _pardonActive = false;
                _sampleCount = 0;
            }

            public void SubmitCurrentFrame()
            {
                float currentTime = Time.unscaledTime - _sessionStartingTime;
                float timeDelta = currentTime - _lastFrameTime;
                bool framerateAlert = timeDelta > (1f / _configuration.alertFps);
                float currentFps = (1f / timeDelta);

                _averageFps = ApproxRollingAverage(_averageFps, currentFps, _sampleCount);
                _sampleCount++;

                if (_pardonActive)
                {
                    _pardonActive = false;
                    framerateAlert = false;
                }

                _ongoingRecord.endSeconds = currentTime;

                if (framerateAlert)
                {
                    if (!_ongoingRegion)
                    {
                        _ongoingRecord.startSeconds = _lastFrameTime;
                        _ongoingRecord.minFps = currentFps;
                    }
                    else if (currentFps < _ongoingRecord.minFps)
                    {
                        _ongoingRecord.minFps = currentFps;
                    }

                    _ongoingRegion = true;
                }
                else if (_ongoingRegion)
                {
                    FinalizeOngoingRecord();
                }

                _lastFrameTime = currentTime;
            }

            public void PardonCurrentFrame()
            {
                _pardonActive = true;
            }

            private void FinalizeOngoingRecord()
            {
                _ongoingRegion = false;
                if (_ongoingRecord.DurationSeconds >= _configuration.alertTimeMinimumSeconds)
                {
                    _alertRecord.AddLast(_ongoingRecord);
                }
            }

            public string ProduceTsvFromRecord()
            {
                StringBuilder tsv = new StringBuilder();
                const char delimeter = '\t';

                tsv.Append("Average FPS: ").Append(_averageFps).AppendLine();

                // Header
                tsv.Append("Start (s)").Append(delimeter);
                tsv.Append("End (s)").Append(delimeter);
                tsv.Append("Min Framerate (fps)").Append(delimeter);
                tsv.AppendLine();

                // Data
                foreach (FrameRecord frameRecord in _alertRecord)
                {
                    tsv.Append(frameRecord.startSeconds).Append(delimeter);
                    tsv.Append(frameRecord.endSeconds).Append(delimeter);
                    tsv.Append(frameRecord.minFps).Append(delimeter);
                    tsv.AppendLine();
                }

                if (_ongoingRegion && _ongoingRecord.DurationSeconds >= _configuration.alertTimeMinimumSeconds)
                {
                    tsv.Append(_ongoingRecord.startSeconds).Append(delimeter);
                    tsv.Append(_ongoingRecord.endSeconds).Append(delimeter);
                    tsv.Append(_ongoingRecord.minFps).Append(delimeter);
                    tsv.AppendLine();
                }

                return tsv.ToString();
            }

            private struct FrameRecord
            {
                public float startSeconds;
                public float endSeconds;
                public float minFps;

                public float DurationSeconds
                {
                    get { return endSeconds - startSeconds; }
                }
            }

            
            private static float ApproxRollingAverage(float avg, float newSample, int sampleCount)
            {
                if (sampleCount <= 0)
                {
                    return newSample;
                }

                avg -= avg / sampleCount;
                avg += newSample / sampleCount;
                return avg;
            }
        }
    }

    public sealed class SessionConfugration
    {
        /// <summary>
        /// What framerate triggers an alert.
        /// </summary>
        public int alertFps = 57;
    
        /// <summary>
        /// How many frames should our fps be below the alert to trigger an alert record.
        /// </summary>
        public int alertTimeMinimumSeconds = 0;
    }
}
