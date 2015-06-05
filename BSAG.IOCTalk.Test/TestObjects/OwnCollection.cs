using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace BSAG.IOCTalk.Test.TestObjects
{
    public class OwnCollection : IList<string>
    {
        private List<string> internalList = new List<string>();


        public IEnumerator GetEnumerator()
        {
            return internalList.GetEnumerator();
        }

        public void Add(string item)
        {
            internalList.Add(item);
        }

        public void Clear()
        {
            internalList.Clear();
        }

        public bool Contains(string item)
        {
            return internalList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return internalList.Remove(item);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return internalList.GetEnumerator();
        }

        public int IndexOf(string item)
        {
            return internalList.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public string this[int index]
        {
            get
            {
                return internalList[index];
            }
            set
            {
                internalList[index] = value;
            }
        }
    }
}
