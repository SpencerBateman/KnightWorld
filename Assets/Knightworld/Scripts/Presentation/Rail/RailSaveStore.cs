using System.IO;
using Knightworld.Core;
using UnityEngine;

namespace Knightworld.Presentation
{
    public static class RailSaveStore
    {
        public static string FilePath => Path.Combine(Application.persistentDataPath, "rail-save.txt");

        public static void Write(RailSession session)
        {
            if (session == null)
                return;
            try
            {
                File.WriteAllText(FilePath, RailSaveCodec.Write(session.Capture()));
            }
            catch (IOException ex)
            {
                Debug.LogWarning("Could not save railroad: " + ex.Message);
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch (IOException ex)
            {
                Debug.LogWarning("Could not reset railroad save: " + ex.Message);
            }
        }

        public static bool TryLoad(out RailSession session)
        {
            session = null;
            try
            {
                if (!File.Exists(FilePath))
                    return false;
                if (!RailSaveCodec.TryRead(File.ReadAllText(FilePath), out var state))
                    return false;
                if (!RailSaveCodec.MatchesMap(state))
                    return false;
                session = RailSession.FromSave(state);
                return session != null;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
