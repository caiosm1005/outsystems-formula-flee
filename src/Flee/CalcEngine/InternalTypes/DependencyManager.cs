﻿using Flee.CalcEngine.PublicTypes;

namespace Flee.CalcEngine.InternalTypes
{

    /// <summary>
    /// Keeps track of our dependencies
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DependencyManager<T>
    {

        /// <summary>
        /// Map of a node and the nodes that depend on it
        /// </summary>
        private readonly Dictionary<T, Dictionary<T, object>> _myDependentsMap;
        private readonly IEqualityComparer<T> _myEqualityComparer;

        /// <summary>
        /// Map of a node and the number of nodes that point to it
        /// </summary>
        private readonly Dictionary<T, int> _myPrecedentsMap;
        public DependencyManager(IEqualityComparer<T> comparer)
        {
            _myEqualityComparer = comparer;
            _myDependentsMap = new Dictionary<T, Dictionary<T, object>>(_myEqualityComparer);
            _myPrecedentsMap = new Dictionary<T, int>(_myEqualityComparer);
        }

        private IDictionary<T, object> CreateInnerDictionary()
        {
            return new Dictionary<T, object>(_myEqualityComparer);
        }

        private IDictionary<T, object> GetInnerDictionary(T tail)
        {
            Dictionary<T, object> value;
            if (_myDependentsMap.TryGetValue(tail, out value) == true)
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        // Create a dependency list with only the dependents of the given tails
        public DependencyManager<T> CloneDependents(T[] tails)
        {
            IDictionary<T, object> seenNodes = CreateInnerDictionary();
            DependencyManager<T> copy = new(_myEqualityComparer);

            foreach (T tail in tails)
            {
                CloneDependentsInternal(tail, copy, seenNodes);
            }

            return copy;
        }

        private void CloneDependentsInternal(T tail, DependencyManager<T> target, IDictionary<T, object> seenNodes)
        {
            if (seenNodes.ContainsKey(tail) == true)
            {
                // We've already added this node so just return
                return;
            }
            else
            {
                // Haven't seen this node yet; mark it as visited
                seenNodes.Add(tail, null);
                target.AddTail(tail);
            }

            IDictionary<T, object> innerDict = GetInnerDictionary(tail);

            // Do the recursive add
            foreach (T head in innerDict.Keys)
            {
                target.AddDepedency(tail, head);
                CloneDependentsInternal(head, target, seenNodes);
            }
        }

        public T[] GetTails()
        {
            T[] arr = new T[_myDependentsMap.Keys.Count];
            _myDependentsMap.Keys.CopyTo(arr, 0);
            return arr;
        }

        public void Clear()
        {
            _myDependentsMap.Clear();
            _myPrecedentsMap.Clear();
        }

        public void ReplaceDependency(T old, T replaceWith)
        {
            Dictionary<T, object> value = _myDependentsMap[old];

            _myDependentsMap.Remove(old);
            _myDependentsMap.Add(replaceWith, value);

            foreach (Dictionary<T, object> innerDict in _myDependentsMap.Values)
            {
                if (innerDict.ContainsKey(old) == true)
                {
                    innerDict.Remove(old);
                    innerDict.Add(replaceWith, null);
                }
            }
        }

        public void AddTail(T tail)
        {
            if (_myDependentsMap.ContainsKey(tail) == false)
            {
                _myDependentsMap.Add(tail, (Dictionary<T, object>)CreateInnerDictionary());
            }
        }

        public void AddDepedency(T tail, T head)
        {
            IDictionary<T, object> innerDict = GetInnerDictionary(tail);

            if (innerDict.ContainsKey(head) == false)
            {
                innerDict.Add(head, head);
                AddPrecedent(head);
            }
        }

        public void RemoveDependency(T tail, T head)
        {
            IDictionary<T, object> innerDict = GetInnerDictionary(tail);
            RemoveHead(head, innerDict);
        }

        private void RemoveHead(T head, IDictionary<T, object> dict)
        {
            if (dict.Remove(head) == true)
            {
                RemovePrecedent(head);
            }
        }

        public void Remove(T[] tails)
        {
            foreach (Dictionary<T, object> innerDict in _myDependentsMap.Values)
            {
                foreach (T tail in tails)
                {
                    RemoveHead(tail, innerDict);
                }
            }

            foreach (T tail in tails)
            {
                _myDependentsMap.Remove(tail);
            }
        }

        public void GetDirectDependents(T tail, List<T> dest)
        {
            Dictionary<T, object> innerDict = (Dictionary<T, object>)GetInnerDictionary(tail);
            dest.AddRange(innerDict.Keys);
        }

        public T[] GetDependents(T tail)
        {
            Dictionary<T, object> dependents = (Dictionary<T, object>)CreateInnerDictionary();
            GetDependentsRecursive(tail, dependents);

            T[] arr = new T[dependents.Count];
            dependents.Keys.CopyTo(arr, 0);
            return arr;
        }

        private void GetDependentsRecursive(T tail, Dictionary<T, object> dependents)
        {
            dependents[tail] = null;
            Dictionary<T, object> directDependents = (Dictionary<T, object>)GetInnerDictionary(tail);

            foreach (T pair in directDependents.Keys)
            {
                GetDependentsRecursive(pair, dependents);
            }
        }

        public void GetDirectPrecedents(T head, IList<T> dest)
        {
            foreach (T tail in _myDependentsMap.Keys)
            {
                Dictionary<T, object> innerDict = (Dictionary<T, object>)GetInnerDictionary(tail);
                if (innerDict.ContainsKey(head) == true)
                {
                    dest.Add(tail);
                }
            }
        }

        private void AddPrecedent(T head)
        {
            int count;
            _myPrecedentsMap.TryGetValue(head, out count);
            _myPrecedentsMap[head] = count + 1;
        }

        private void RemovePrecedent(T head)
        {
            int count = _myPrecedentsMap[head] - 1;

            if (count == 0)
            {
                _myPrecedentsMap.Remove(head);
            }
            else
            {
                _myPrecedentsMap[head] = count;
            }
        }

        public bool HasPrecedents(T head)
        {
            return _myPrecedentsMap.ContainsKey(head);
        }

        public bool HasDependents(T tail)
        {
            Dictionary<T, object> innerDict = (Dictionary<T, object>)GetInnerDictionary(tail);
            return innerDict.Count > 0;
        }

        private static string FormatValues(ICollection<T> values)
        {
            string[] strings = new string[values.Count];
            T[] keys = new T[values.Count];
            values.CopyTo(keys, 0);

            for (int i = 0; i <= keys.Length - 1; i++)
            {
                strings[i] = keys[i].ToString();
            }

            if (strings.Length == 0)
            {
                return "<empty>";
            }
            else
            {
                return string.Join(",", strings);
            }
        }

        /// <summary>
        ///  Add all nodes that don't have any incoming edges into a queue
        /// </summary>
        /// <param name="rootTails"></param>
        /// <returns></returns>
        public Queue<T> GetSources(T[] rootTails)
        {
            Queue<T> q = new();

            foreach (T rootTail in rootTails)
            {
                if (HasPrecedents(rootTail) == false)
                {
                    q.Enqueue(rootTail);
                }
            }

            return q;
        }

        public IList<T> TopologicalSort(Queue<T> sources)
        {
            IList<T> output = new List<T>();
            List<T> directDependents = new();

            while (sources.Count > 0)
            {
                T n = sources.Dequeue();
                output.Add(n);

                directDependents.Clear();
                GetDirectDependents(n, directDependents);

                foreach (T m in directDependents)
                {
                    RemoveDependency(n, m);

                    if (HasPrecedents(m) == false)
                    {
                        sources.Enqueue(m);
                    }
                }
            }

            if (output.Count != Count)
            {
                throw new CircularReferenceException();
            }

            return output;
        }

#if DEBUG
        public string Precedents
        {
            get
            {
                List<string> list = new();

                foreach (KeyValuePair<T, int> pair in _myPrecedentsMap)
                {
                    list.Add(pair.ToString());
                }

                return string.Join(Environment.NewLine, list.ToArray());
            }
        }
#endif

        public string DependencyGraph
        {
            get
            {
                string[] lines = new string[_myDependentsMap.Count];
                int index = 0;

                foreach (KeyValuePair<T, Dictionary<T, object>> pair in _myDependentsMap)
                {
                    T key = pair.Key;
                    string s = FormatValues(pair.Value.Keys);
                    lines[index] = $"{key} -> {s}";
                    index += 1;
                }

                return string.Join(Environment.NewLine, lines);
            }
        }

        public int Count => _myDependentsMap.Count;
    }

}

