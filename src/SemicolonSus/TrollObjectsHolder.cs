using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SemicolonSus.Delegates;

namespace SemicolonSus {

    /// <summary>
    /// A class for handling items and faciltating operations on scanning and overwriting.
    /// </summary>
    internal class TrollObjectsHolder {

        /// <summary>
        /// A constant enumeration for showing the operation.
        /// </summary>
        internal enum OperationType {
            /// <summary>
            /// Scans the items.
            /// </summary>
            Scan,

            /// <summary>
            /// Trolls the items (or overwrites items with a greek question marks.)
            /// </summary>
            Trolling,

            /// <summary>
            /// De-trolls the items (or overwrites items with a semicolons.)
            /// </summary>
            DeTrolling
        }

        /// <summary>
        /// A constant enumeration on selecting the operation.
        /// </summary>
        internal enum TrollType {
            /// <summary>
            /// Trolls the items (or overwrites items with a greek question marks.)
            /// </summary>
            Troll,

            /// <summary>
            /// De-trolls the items (or overwrites items with a semicolons.)
            /// </summary>
            DeTroll
        }

        internal event OperationCompletedDelegate OperationCompleted;
        internal event ProgressDetailsUpdateDelegate ProgressDetailsUpdate;
        internal event EventHandler ItemsUpdated;

        /// <summary>
        /// Gets the number of items in the object.
        /// </summary>
        internal int Count {
            get => _items.Count;
        }

        /// <summary>
        /// Gets the current status if the object is running.
        /// </summary>
        internal bool IsRunning { get; private set; }

        private readonly List<ListViewItem> _items = new List<ListViewItem>();
        private PleaseWaitDialog dialog;
        private CancellationTokenSource _cancel = new CancellationTokenSource();
        private readonly ListView lv;

        /// <summary>
        /// Initiates a new <see cref="TrollObjectsHolder"/>.
        /// </summary>
        /// <param name="lv">The current <see cref="ListView"/> object.</param>
        public TrollObjectsHolder(ListView lv) {
            this.lv = lv;
        }

        /// <summary>
        /// Adds the items, maximum of 1000 items are allowed.
        /// </summary>
        /// <param name="objects">An array of string containing both file and directory paths.</param>
        /// <param name="pattern">A pattern for searching files.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        internal void Add(string[] objects, string[] pattern) {
            if (IsRunning) throw new InvalidOperationException("Couldn't add while other operation is running.");
            if (objects == null) throw new ArgumentNullException("objects");
            if (_items.Count >= 1000) throw new ArgumentException("Items are at the maximum capacity.");

            Task.Run(() => {
                IsRunning = true;
                List<string> files = new List<string>();
                foreach (string item in objects) {
                    try {
                        if (File.Exists(item) && pattern.Contains("*" + new FileInfo(item).Extension)) {
                            files.Add(item);
                        } else if (Directory.Exists(item)) {
                            files.AddRange(SearchFiles(item, pattern));
                        }
                    } catch {
                        // Ignore
                    }
                }

                foreach (string file in files) {
                    if (!_items.Exists((f) => f.SubItems[2].Text == file)) {
                        ListViewItem lvi = new ListViewItem(new FileInfo(file).Name);
                        lvi.SubItems.Add("---");
                        lvi.SubItems.Add("---");
                        lvi.SubItems.Add(file);
                        _items.Add(lvi);
                    }
                    if (_items.Count == 1000) break;
                }
                IsRunning = false;
                UpdateListView();
                CloseDialog();
            });
            ShowDialog();
        }

        /// <summary>
        /// Removes checked items.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        internal void RemoveCheckedItems() {
            if (IsRunning) throw new InvalidOperationException("Couldn't remove while other operation is running.");
            if (_items.Count > 0) {
                Task.Run(() => {
                    IsRunning = true;
                    lv.Invoke(new Action(() => {
                        for (int i = _items.Count - 1; i >= 0; i--)
                            if (_items[i].Checked)
                                _items.RemoveAt(i);

                        IsRunning = false;
                    }));
                    UpdateListView();
                    CloseDialog();
                });
                ShowDialog();
            }
        }

        /// <summary>
        /// Cancels the current operation.
        /// </summary>
        internal void Cancel() {
            if (IsRunning) _cancel.Cancel();
        }

        /// <summary>
        /// Initiates to scan items for troll (or greek question mark) prescence.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void ScanIt() {
            if (IsRunning) throw new InvalidOperationException("Couldn't start while other operation is running.");
            if (_items.Count == 0) throw new InvalidOperationException("Items are empty.");
            Task.Run(() => {
                IsRunning = true;
                for (int i = 0; i < _items.Count; i++) {
                    if (_cancel.Token.IsCancellationRequested) {
                        ReloadCancel();
                        ReLabelAsCancel();

                        ProgressDetailsUpdate?.Invoke(i, _items.Count, "Process has been cancelled!");
                        OperationCompleted?.Invoke(OperationType.Scan, true, null);
                        IsRunning = false;
                        return;
                    }

                    ProgressDetailsUpdate?.Invoke(i, _items.Count, "Scanning " + _items[i].SubItems[0].Text + "...");
                    string output = "";
                    try {
                        string path = _items[i].ListView.Invoke(new Func<string>(() => _items[i].SubItems[3].Text)).ToString();
                        TrollClass.LockFile(path);
                        int[] res = TrollClass.CheckTrollPrescence();

                        output = res[0] != -1 ? res[0].ToString() + "%" : "Failed.";
                        output += res[0] != -1 ? " (" + res[1].ToString() + " troll character/s found!)" : "";
                        TrollClass.UnlockFile();
                        ProgressDetailsUpdate?.Invoke(i + 1, _items.Count, "Scanned " + _items[i].SubItems[0].Text);
                    } catch (Exception ex) {
                        output = "Can't scan!";
                        ProgressDetailsUpdate?.Invoke(i + 1, _items.Count, "Failed to scan " + ex.Message);
                    }
                    _items[i].ListView.Invoke(new Action(() => {
                        _items[i].SubItems[1].Text = output;
                        _items[i].SubItems[2].Text = "Scanned";
                    }));
                }
                IsRunning = false;
                ProgressDetailsUpdate?.Invoke(_items.Count, _items.Count, "Process completed.");
                OperationCompleted?.Invoke(OperationType.Scan, false, null);
            });
        }

        /// <summary>
        /// Initiates to troll or de-troll items.
        /// </summary>
        /// <param name="type">A type of operation to use.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void TrollInit(TrollType type) {
            if (IsRunning) throw new InvalidOperationException("Couldn't start while other operation is running.");
            if (_items.Count == 0) throw new InvalidOperationException("Items are empty.");

            Task.Run(() => {
                bool randomizedBool = Properties.Settings.Default.IsRandomized;
                int randomizedChance = Properties.Settings.Default.RandomizedChance;

                IsRunning = true;
                string trollType = type == TrollType.Troll ? "Troll" : "De-troll";
                for (int i = 0; i < _items.Count; i++) {
                    if (_cancel.Token.IsCancellationRequested) {
                        if (type == TrollType.Troll) {
                            for (int j = i; j >= 0; j--) {
                                string path = _items[i].ListView.Invoke(new Func<string>(() => _items[j].SubItems[3].Text)).ToString();
                                TrollClass.LockFile(path);
                                TrollClass.WriteTroll(false);
                                TrollClass.UnlockFile();
                            }
                        }
                        ProgressDetailsUpdate?.Invoke(i, _items.Count, "Process has been cancelled!");
                        ReLabelAsCancel();
                        OperationCompleted?.Invoke(type == TrollType.Troll ? OperationType.Trolling : OperationType.DeTrolling, true, null);
                        IsRunning = false;
                        return;
                    }

                    ProgressDetailsUpdate?.Invoke(i, _items.Count, trollType + "ing " + _items[i].SubItems[0].Text + "...");
                    string output = "";

                    try {
                        string path = _items[i].ListView.Invoke(new Func<string>(() => _items[i].SubItems[3].Text)).ToString();
                        TrollClass.LockFile(path);
                        TrollClass.WriteTroll(type == TrollType.Troll, randomizedBool, randomizedChance);
                        int[] res = TrollClass.CheckTrollPrescence();

                        output = res[0] != -1 ? res[0].ToString() + "%" : "Failed.";
                        output += res[0] != -1 ? " (" + res[1].ToString() + " troll character/s found!)" : "";
                        TrollClass.UnlockFile();
                        ProgressDetailsUpdate?.Invoke(i + 1, _items.Count, trollType + "ed " + _items[i].SubItems[0].Text);
                    } catch (Exception ex) {
                        output = "Can't write!";
                        ProgressDetailsUpdate?.Invoke(i + 1, _items.Count, "Failed to write: " + ex.Message);
                    }
                    _items[i].ListView.Invoke(new Action(() => {
                        _items[i].SubItems[1].Text = output;
                        _items[i].SubItems[2].Text = trollType + "ed";
                    }));

                }
                IsRunning = false;
                ProgressDetailsUpdate?.Invoke(_items.Count, _items.Count, "Process completed.");
                OperationCompleted?.Invoke(type == TrollType.Troll ? OperationType.Trolling : OperationType.DeTrolling, false, null);
            });
        }

        /// <summary>
        /// Re-label items' texts as cancelled.
        /// </summary>
        private void ReLabelAsCancel() {
            lv.Invoke(new Action(() => {
                foreach (ListViewItem lvi in _items) {
                    lvi.SubItems[1].Text = "---";
                    lvi.SubItems[2].Text = "Cancelled";
                }
            }));
        }

        /// <summary>
        /// Searches the files from the directory.
        /// </summary>
        /// <param name="path">A string containing the directory path.</param>
        /// <param name="patterns">A search pattern on getting specific files.</param>
        /// <returns>Returns an array of strings containing file paths.</returns>
        private string[] SearchFiles(string path, string[] patterns) {
            try {
                if (patterns == null) return Directory.EnumerateFiles(path).ToArray();
                List<string> res = new List<string>();
                foreach (string pattern in patterns)
                    res.AddRange(Directory.EnumerateFiles(path, pattern));
                return res.ToArray();
            } catch {
                return new string[0];
            }
        }

        /// <summary>
        /// Reloads the cancel after initiating.
        /// </summary>
        private void ReloadCancel() {
            _cancel.Dispose();
            _cancel = new CancellationTokenSource();
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        private void ShowDialog() {
            if (dialog == null) {
                dialog = new PleaseWaitDialog();
                dialog.CreateControl();
                dialog.ShowDialog();
            }
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        private void CloseDialog() {
            if (dialog != null) {
                dialog.Invoke(new Action(() => {
                    dialog.Dispose();
                    dialog = null;
                }));
            }
        }

        /// <summary>
        /// Updates the <see cref="ListView"/> items.
        /// </summary>
        private void UpdateListView() {
            lv.Invoke(new Action(() => {
                lv.BeginUpdate();
                lv.Items.Clear();
                lv.Items.AddRange(_items.ToArray());
                lv.EndUpdate();
                ItemsUpdated?.Invoke(this, EventArgs.Empty);
            }));
        }
    }
}
