﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Indexer.Helpers;
using Indexer.Tokens;

namespace Indexer.Indexes
{
    public class InvertedHashIndex : IInvertedIndex
    {
        private readonly ITokenizer tokenizer;
        private readonly ConcurrentDictionary<int, HashSet<StoredResult>> dictionary;

        public InvertedHashIndex(ITokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
            this.dictionary = new ConcurrentDictionary<int, HashSet<StoredResult>>();
        }

        public void Add(string line, int rowNumber, string document)
        {
            var tokens = this.tokenizer.GetTokens(line);
            foreach (var token in tokens)
            {
                this.AddToken(token, rowNumber, document);
            }
        }

        public IList<StoredResult> Find(string query)
        {
            var tokens = this.tokenizer.GetTokens(query);
            var count = tokens.Count;
            if (count == 1)
            {
                if (this.dictionary.TryGetValue(StringHelper.GetHashCode(tokens[0].Term), out var hashSet))
                {
                    return hashSet.ToList();
                }
            }
            else
            {
                var sets = new HashSet<StoredResult>[count];
                for (var i = 0; i < count; i++)
                {
                    var term = tokens[i].Term;
                    if (!this.dictionary.TryGetValue(StringHelper.GetHashCode(term), out sets[i]))
                    {
                        return new List<StoredResult>();
                    }
                }

                return GetPhraseMatches(tokens.Select(x => x.Term).ToArray(), sets);
            }

            return new List<StoredResult>();
        }

        private static IList<StoredResult> GetPhraseMatches(string[] terms, HashSet<StoredResult>[] sets)
        {
            var resultList = new List<StoredResult>();
            var suffixesCount = terms.Length;
            var suffix = terms[0];
            var currentOffset = suffix.Length;
            for (var i = 0; i < sets[0].Count; i++)
            {
                foreach (var storedResult in sets[0])
                {
                    for (var j = 1; j < suffixesCount; j++)
                    {
                        var expectedNextResult = new StoredResult
                        {
                            Document = storedResult.Document,
                            RowNumber = storedResult.RowNumber,
                            ColNumber = storedResult.ColNumber + currentOffset
                        };
                        if (!sets[j].Contains(expectedNextResult))
                        {
                            break;
                        }

                        currentOffset += terms[j].Length;
                        if (j == suffixesCount - 1)
                        {
                            resultList.Add(storedResult);
                        }
                    }
                }
            }

            return resultList;
        }

        private void AddToken(Token token, int rowNumber, string document)
        {
            var startColNumber = token.Position;
            var term = token.Term;
            var length = term.Length;
            var prefixStoredResult = new StoredResult
            {
                ColNumber = startColNumber,
                Document = document,
                RowNumber = rowNumber
            };
            this.AddTerm(term, prefixStoredResult);
            for (var i = 0; i < term.Length - 1; i++)
            {
                var suffixStoredResult = new StoredResult
                {
                    ColNumber = startColNumber + i + 1,
                    Document = document,
                    RowNumber = rowNumber
                };

                var suffix = term.Substring(i + 1, length - i - 1);
                var prefix = term.Substring(0, i + 1);
                this.AddTerm(suffix, suffixStoredResult);
                this.AddTerm(prefix, prefixStoredResult);
            }
        }

        private void AddTerm(string term, StoredResult result)
        {
            var hashCode = StringHelper.GetHashCode(term);
            if (!this.dictionary.ContainsKey(hashCode))
            {
                this.dictionary.TryAdd(hashCode, new HashSet<StoredResult>());
            }

            this.dictionary[hashCode].Add(result);
        }
    }
}