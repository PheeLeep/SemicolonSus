using System;
using System.Collections.Generic;
using System.IO;

namespace SemicolonSus {

    /// <summary>
    /// A static class on performing a scan and character replacement.
    /// </summary>
    internal static class TrollClass {

        /// <summary>
        /// A standard semicolon character.
        /// </summary>
        readonly static char Semicolon = ';'; 

        /// <summary>
        /// A special character that looks like a semicolon.
        /// </summary>
        readonly static char GreekQuestionMark = ';';

        /// <summary>
        /// Gets the boolean if the file was locked.
        /// </summary>
        internal static bool OnLock { get; private set; }

        private static FileStream _stream;
        private static readonly Random _random = new Random();

        /// <summary>
        /// Locks the file.
        /// </summary>
        /// <param name="path">A string containing the path of the selected file.</param>
        /// <exception cref="InvalidOperationException"></exception>
        internal static void LockFile(string path) {
            if (OnLock) throw new InvalidOperationException("Access Denied! A current file is still lock.");
            _stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            OnLock = true;
        }

        /// <summary>
        /// Scans the file for greek question marks' prescence.
        /// </summary>
        /// <returns>Returns an array of integer with a size of two. 
        /// Index 0 for percentage, and index 1 for number of greek question mark prescence.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        internal static int[] CheckTrollPrescence() {
            if (!OnLock) throw new InvalidOperationException("No file was locked.");
            StreamReader _reader = new StreamReader(_stream);
            int[] res = new int[2];
            int totalPrescence = 0;
            int greekQuestionPrescence = 0;

            while (!_reader.EndOfStream) {
                char c = (char)_reader.Read();
                if (c == Semicolon || c == GreekQuestionMark) totalPrescence++;
                if (c == GreekQuestionMark) greekQuestionPrescence++;
            }

            _stream.Position = 0;
            res[0] = totalPrescence > 0 ? (greekQuestionPrescence * 100) / totalPrescence : -1;
            res[1] = greekQuestionPrescence;
            return res;
        }

        /// <summary>
        /// Overwrites the locked file with either greek question mark or semicolon.
        /// </summary>
        /// <param name="isTroll">Sets true to overwrite with a greek question mark, 
        /// false to overwrite with a semicolon.</param>
        /// <param name="isRandomized">Sets true to randomize overwrite the character. Discards this
        /// parameter if 'isTroll' is set to false.</param>
        /// <param name="chances">Sets the chance of character to be overwritten, Discards this
        /// parameter if 'isTroll' or 'isRandomized' is set to false.</param>
        /// <exception cref="InvalidOperationException"></exception>
        internal static void WriteTroll(bool isTroll, bool isRandomized = false, int chances = 90) {
            if (!OnLock) throw new InvalidOperationException("No file was locked.");
            StreamReader _reader = new StreamReader(_stream);
            StreamWriter _writer = new StreamWriter(_stream);
            List<string> l = new List<string>();
            string s;

            while ((s = _reader.ReadLine()) != null) {
                if (!isTroll) {
                    s = s.Replace(GreekQuestionMark, Semicolon);
                } else {
                    if (!isRandomized) {
                        s = s.Replace(Semicolon, GreekQuestionMark);
                    } else {
                        char[] cArr = s.ToCharArray();
                        for (int i = 0; i < cArr.Length; i++)
                            if (cArr[i] == Semicolon && _random.Next(0, 101) >= chances)
                                cArr[i] = GreekQuestionMark;
                        s = new string(cArr);
                    }
                }
                l.Add(s);
            }
            _stream.SetLength(0); // Clears all data from the stream before writing from the 'l' list.
            _stream.Flush();
            for (int i = 0; i < l.Count; i++)
                _writer.WriteLine(l[i]);
            _writer.Flush();
            _stream.Position = 0;
        }

        /// <summary>
        /// Unlocks the file.
        /// </summary>
        internal static void UnlockFile() {
            if (!OnLock) return;
            _stream.Close();
            _stream = null;
            OnLock = false;
        }
    }
}
