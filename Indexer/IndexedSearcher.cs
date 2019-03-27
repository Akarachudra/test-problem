﻿using System.Collections.Generic;

namespace Indexer
{
    public class IndexedSearcher : ISearchService
    {
        private readonly string filesPath;

        public IndexedSearcher(string filesPath)
        {
            this.filesPath = filesPath;
        }

        public IList<StoredResult> Find(string term)
        {
            throw new System.NotImplementedException();
        }
    }
}