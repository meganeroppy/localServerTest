using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Dissonance.Editor.Windows.Update
{
    [InitializeOnLoad]
    public class UpdateLauncher
    {
        private const string CheckForNewVersionKey = "placeholder_dissonance_update_checkforlatestversion";
        private const string UpdaterToggleMenuItemPath = "Window/Dissonance/Check For Updates";

        private static readonly string StatePath = Path.Combine(DissonanceRootPath.BaseResourcePath, ".UpdateState.json");

        private static IEnumerator<bool> _updateCheckInProgress;

        /// <summary>
        /// This method will run as soon as the editor is loaded (with Dissonance in the project)
        /// </summary>
        static UpdateLauncher()
        {
            //Launching the window here caused some issues (presumably it's a bit too early for Unity to handle). Instead we'll wait until the first update call to do it.
            EditorApplication.update += Update;

            //Unity loses the checkbox which creates a misleading API (user thinks updater is disabled, but it's not). Update it every frame to make it really stick!
            EditorApplication.update += UpdateMenuItemToggle;
        }

        [MenuItem(UpdaterToggleMenuItemPath, priority = 500)]
        public static void ToggleUpdateCheck()
        {
            var wasEnabled = GetUpdaterEnabled();
            var enabled = !wasEnabled;
            SetUpdaterEnabled(enabled);

            if (enabled)
            {
                var state = GetUpdateState();
                SetUpdateState(new UpdateState(state.ShownForVersion, DateTime.UtcNow));

                EditorApplication.update += Update;
            }
        }

        private static IEnumerable<bool> CheckUpdateVersion()
        {
            //Exit if the update check is not enabled
            if (!GetUpdaterEnabled())
                yield return false;

            //Exit if the next update isn't due yet
            var state = GetUpdateState();
            if (state.NextCheck >= DateTime.UtcNow)
                yield return false;

            //setup some helpers for later
            var random = new System.Random();
            Action failed = () => SetUpdateState(new UpdateState(state.ShownForVersion, DateTime.UtcNow + TimeSpan.FromMinutes(random.Next(10, 70))));
            Action success = () => SetUpdateState(new UpdateState(state.ShownForVersion, DateTime.UtcNow + TimeSpan.FromHours((random.Next(12, 72)))));

            //Begin downloading the manifest of all Dissonance updates
            var request = UnityWebRequest.Get(string.Format("https://placeholder-software.co.uk/dissonance/releases/latest-published.html{0}", EditorMetadata.GetQueryString()));
            request.Send();

            //Wait until request is complete
            while (!request.isDone && !request.isError)
                yield return true;

            //If it's an error give up and schedule the next check fairly soon
            if (request.isError)
            {
                request.Dispose();
                failed();
                yield return false;
            }

            //Get the response bytes and discard the request
            var bytes = request.downloadHandler.data;
            request.Dispose();

            //Parse the response data. If we fail give up and schedule the next check fairly soon
            SemanticVersion latest;
            if (!TryParse(bytes, out latest) || latest == null)
            {
                failed();
                yield return false;
            }
            else
            {
                //Check if we've already shown the window for a greater version
                if (latest.CompareTo(state.ShownForVersion) <= 0)
                {
                    success();
                    yield return false;
                }

                //Check if the new version is greater than the currently installed version
                if (latest.CompareTo(DissonanceComms.Version) <= 0)
                {
                    success();
                    yield return false;
                }

                //Update the state so that the window does not show up again for this version
                SetUpdateState(new UpdateState(latest, DateTime.UtcNow + TimeSpan.FromHours((random.Next(12, 72)))));
                UpdateWindow.Show(latest, DissonanceComms.Version);
                success();
            }
        }

        private static void Update()
        {
            //Reapply the current updater state to make sure everything is up to date
            UpdateMenuItemToggle();

            if (_updateCheckInProgress == null)
                _updateCheckInProgress = CheckUpdateVersion().GetEnumerator();

            //Pump updater until it's done
            if (!_updateCheckInProgress.MoveNext() || !_updateCheckInProgress.Current)
            {
                _updateCheckInProgress = null;

                // ReSharper disable once DelegateSubtraction (Justification: I know what I'm doing... famous last words)
                EditorApplication.update -= Update;
            }
        }

        private static bool TryParse(byte[] bytes, [CanBeNull] out SemanticVersion parsed)
        {
            try
            {
                // The received data is a root level array. Wrap it in an object which gives the root array a name
                var str = Encoding.UTF8.GetString(bytes);
                parsed = JsonUtility.FromJson<SemanticVersion>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                parsed = null;
                return false;
            }
        }

        private static UpdateState GetUpdateState()
        {
            if (!File.Exists(StatePath))
            {
                // State path does not exist at all so create the default
                var state = new UpdateState(new SemanticVersion(), DateTime.UtcNow);
                SetUpdateState(state);
                return state;
            }
            else
            {
                //Read the state from the file
                using (var reader = File.OpenText(StatePath))
                {
                    var state = JsonUtility.FromJson<UpdateState>(reader.ReadToEnd());
                    return state;
                }
            }
        }

        private static void SetUpdateState([CanBeNull] UpdateState state)
        {
            if (state == null)
            {
                //Clear installer state
                File.Delete(StatePath);
            }
            else
            {
                using (var writer = File.CreateText(StatePath))
                    writer.Write(JsonUtility.ToJson(state));
            }
        }

        internal static void SetUpdaterEnabled(bool enabled)
        {
            EditorPrefs.SetBool(CheckForNewVersionKey, enabled);
            UpdateMenuItemToggle();
        }

        private static void UpdateMenuItemToggle()
        {

            // This does not work, seemingly due to a Unity bug.
            // The state gets lost at random times (even setting it every frame isn't good enough).
            // Better to have no UI indication than a misleading one!
            //    var enabled = GetUpdaterEnabled();
            //    Menu.SetChecked(UpdaterToggleMenuItemPath, enabled);
        }

        internal static bool GetUpdaterEnabled()
        {
            if (!EditorPrefs.HasKey(CheckForNewVersionKey))
                return true;

            return EditorPrefs.GetBool(CheckForNewVersionKey);
        }

        [Serializable] private class UpdateState
        {
            [SerializeField, UsedImplicitly] private SemanticVersion _shownForVersion;
            [SerializeField, UsedImplicitly] private long _nextCheckFileTime;

            public SemanticVersion ShownForVersion
            {
                get { return _shownForVersion; }
            }

            public DateTime NextCheck
            {
                get { return DateTime.FromFileTimeUtc(_nextCheckFileTime); }
            }

            public UpdateState(SemanticVersion version, DateTime nextCheck)
            {
                _shownForVersion = version;
                _nextCheckFileTime = nextCheck.ToFileTimeUtc();
            }

            public override string ToString()
            {
                return _shownForVersion.ToString();
            }
        }
    }
}