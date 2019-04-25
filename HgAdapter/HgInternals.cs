﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace HgAdapter {

    // http://mercurial.selenic.com/wiki/FileFormats

    class HgInternals {
        string _repoPath;
        Logger _logger;

        public HgInternals(string repoPath, Logger logger) {
            _repoPath = repoPath;
            _logger = logger;
        }

        public bool WaitWhileInTransaction() {
            var wasInTransaction = false;
            var storePath = GetStorePath();
            var lockPath = Path.Combine(storePath, "lock");
            var journalPath = Path.Combine(storePath, "journal");

            while(File.Exists(lockPath) || File.Exists(journalPath)) {
                wasInTransaction = true;
                _logger.PutToFile("transaction in progress, waiting...");
                Thread.Sleep(1000);
            }

            return wasInTransaction;
        }

        public bool HasRepoChangedSince(DateTime date) {
            var wasInTransaction = WaitWhileInTransaction();
            if(wasInTransaction)
                return true;

            var prevJournalPath = Path.Combine(GetStorePath(), "undo");
            if(!File.Exists(prevJournalPath))
                return true;

            var transactionDate = GetFileChangeTime(prevJournalPath);
            var changed = transactionDate > date;
            if(!changed)
                _logger.PutToFile("repo not changed (last transaction at " + transactionDate.ToString("s") + ")");
            return changed;
        }

        static DateTime GetFileChangeTime(string path) {
            try {
                return Win32.GetFileChangeTime(path);
            } catch {
                return File.GetLastWriteTime(path);
            }
        }

        string GetStorePath() {
            return Path.Combine(_repoPath, ".hg/store");
        }


    }

}
