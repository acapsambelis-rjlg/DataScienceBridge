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
        string[] GetImageColumnNames();
    }

    public interface IStreamingDataSource
    {
        string GetCsvHeader();
        IEnumerable<string> StreamCsvRows();
        string[] GetImageColumnNames();
    }

    internal class StringDataSource : IInMemoryDataSource
    {
        private readonly string _csv;
        private string[] _lines;
        private readonly string[] _imageColumns;

        public StringDataSource(string csv, string[] imageColumns = null)
        {
            _csv = csv;
            _imageColumns = imageColumns ?? new string[0];
        }

        public string[] GetImageColumnNames() { return _imageColumns; }

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

    public class DataQueue<T> : IInMemoryDataSource, IStreamingDataSource where T : class
    {
        private readonly Queue<T> _items = new Queue<T>();
        private IEnumerable<T> _lazySource;
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

        public void SetSource(IEnumerable<T> source)
        {
            if (_locked)
                throw new InvalidOperationException("Cannot set source while the DataQueue is being streamed.");
            _lazySource = source;
        }

        public int Count { get { return _items.Count; } }

        public Type ItemType { get { return typeof(T); } }

        public bool IsConsumed { get { return _snapshotCount > 0 && _items.Count == 0 && _lazySource == null; } }

        public void Clear()
        {
            if (_locked)
                throw new InvalidOperationException("Cannot clear while the DataQueue is being streamed.");
            _items.Clear();
            _lazySource = null;
            _snapshotCount = 0;
        }

        public int LineCount
        {
            get
            {
                _snapshotCount = _items.Count;
                return _snapshotCount + 1;
            }
        }

        public IEnumerable<string> StreamCsvLines()
        {
            _snapshotCount = _items.Count;
            _locked = true;

            yield return GetCsvHeader();

            int remaining = _snapshotCount;
            while (remaining > 0 && _items.Count > 0)
            {
                var item = _items.Dequeue();
                remaining--;
                yield return SerializeRow(item);
            }

            _locked = false;
        }

        public string GetCsvHeader()
        {
            var headerParts = new List<string>();
            foreach (var fp in _flatProps)
                headerParts.Add(fp.ColumnName);
            return string.Join(",", headerParts);
        }

        public IEnumerable<string> StreamCsvRows()
        {
            _locked = true;

            if (_lazySource != null)
            {
                foreach (var item in _lazySource)
                    yield return SerializeRow(item);
            }
            else
            {
                while (_items.Count > 0)
                {
                    var item = _items.Dequeue();
                    yield return SerializeRow(item);
                }
            }

            _locked = false;
        }

        private string SerializeRow(T item)
        {
            if (PythonVisibleHelper.PrepareItem != null)
                PythonVisibleHelper.PrepareItem(item);
            try
            {
                var vals = new List<string>();
                foreach (var fp in _flatProps)
                {
                    var val = fp.GetValue(item);
                    string s;
                    if (PythonVisibleHelper.IsImageType(fp.LeafType))
                    {
                        if (val is Bitmap bmp)
                            s = PythonVisibleHelper.BitmapToBase64(bmp);
                        else if (val is Image img)
                            using (var tmp = new Bitmap(img))
                                s = PythonVisibleHelper.BitmapToBase64(tmp);
                        else
                            s = "";
                    }
                    else if (PythonVisibleHelper.IsDictionaryType(fp.LeafType))
                    {
                        s = PythonVisibleHelper.DictionaryToJson(val);
                    }
                    else
                        s = val != null ? val.ToString() : "";
                    if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                        s = "\"" + s.Replace("\"", "\"\"") + "\"";
                    vals.Add(s);
                }
                return string.Join(",", vals);
            }
            finally
            {
                if (PythonVisibleHelper.ReleaseItem != null)
                    PythonVisibleHelper.ReleaseItem(item);
            }
        }

        public string[] GetColumnNames()
        {
            var names = new string[_flatProps.Length];
            for (int i = 0; i < _flatProps.Length; i++)
                names[i] = _flatProps[i].ColumnName;
            return names;
        }

        public string[] GetImageColumnNames()
        {
            var result = new List<string>();
            foreach (var fp in _flatProps)
            {
                if (PythonVisibleHelper.IsImageType(fp.LeafType))
                    result.Add(fp.ColumnName);
            }
            return result.ToArray();
        }

        public FlattenedProperty[] GetFlattenedProperties()
        {
            return _flatProps;
        }
    }
}
