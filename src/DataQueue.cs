using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace RJLG.IntelliSEM.Data.PythonDataScience
{
    public interface IInMemoryDataSource
    {
        int LineCount { get; }
        IEnumerable<string> StreamCsvLines();
    }

    internal class StringDataSource : IInMemoryDataSource
    {
        private readonly string _csv;
        private string[] _lines;

        public StringDataSource(string csv)
        {
            _csv = csv;
        }

        private string[] GetLines()
        {
            if (_lines == null)
                _lines = _csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return _lines;
        }

        public int LineCount { get { return GetLines().Length; } }

        public IEnumerable<string> StreamCsvLines()
        {
            return GetLines();
        }
    }

    public class DataQueue<T> : IInMemoryDataSource where T : class
    {
        private readonly Queue<T> _items = new Queue<T>();
        private FlattenedProperty[] _flatProps;
        private bool _locked;
        private int _snapshotCount;

        public DataQueue()
        {
            var props = PythonVisibleHelper.GetFlattenedProperties(typeof(T));
            _flatProps = props.ToArray();
        }

        public void Enqueue(T item)
        {
            if (_locked)
                throw new InvalidOperationException("Cannot enqueue while the DataQueue is being streamed.");
            _items.Enqueue(item);
        }

        public void EnqueueRange(IEnumerable<T> items)
        {
            if (_locked)
                throw new InvalidOperationException("Cannot enqueue while the DataQueue is being streamed.");
            foreach (var item in items)
                _items.Enqueue(item);
        }

        public int Count { get { return _items.Count; } }

        public Type ItemType { get { return typeof(T); } }

        public bool IsConsumed { get { return _snapshotCount > 0 && _items.Count == 0; } }

        public void Clear()
        {
            if (_locked)
                throw new InvalidOperationException("Cannot clear while the DataQueue is being streamed.");
            _items.Clear();
            _snapshotCount = 0;
        }

        public int LineCount
        {
            get
            {
                _snapshotCount = _items.Count;
                _locked = true;
                return _snapshotCount + 1;
            }
        }

        public IEnumerable<string> StreamCsvLines()
        {
            if (!_locked)
            {
                _snapshotCount = _items.Count;
                _locked = true;
            }

            var headerParts = new List<string>();
            foreach (var fp in _flatProps)
                headerParts.Add(fp.ColumnName);
            yield return string.Join(",", headerParts);

            int remaining = _snapshotCount;
            while (remaining > 0 && _items.Count > 0)
            {
                var item = _items.Dequeue();
                remaining--;
                yield return SerializeRow(item);
            }

            _locked = false;
        }

        private string SerializeRow(T item)
        {
            var vals = new List<string>();
            foreach (var fp in _flatProps)
            {
                var val = fp.GetValue(item);
                string s;
                if (PythonVisibleHelper.IsImageType(fp.LeafType) && val is Bitmap bmp)
                    s = PythonVisibleHelper.BitmapToBase64(bmp);
                else
                    s = val != null ? val.ToString() : "";
                if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                    s = "\"" + s.Replace("\"", "\"\"") + "\"";
                vals.Add(s);
            }
            return string.Join(",", vals);
        }

        public string[] GetColumnNames()
        {
            var names = new string[_flatProps.Length];
            for (int i = 0; i < _flatProps.Length; i++)
                names[i] = _flatProps[i].ColumnName;
            return names;
        }

        public FlattenedProperty[] GetFlattenedProperties()
        {
            return _flatProps;
        }
    }
}
