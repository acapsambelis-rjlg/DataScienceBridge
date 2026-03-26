_csv.field_size_limit(2**31 - 1)
class _StreamRow:
    __slots__ = ('_data',)
    def __init__(self, data):
        object.__setattr__(self, '_data', data)
    def __getattr__(self, name):
        _data = object.__getattribute__(self, '_data')
        if name in _data:
            return _data[name]
        raise AttributeError(f"Row has no field '{name}'")
    def __getitem__(self, key):
        return object.__getattribute__(self, '_data')[key]
    def __repr__(self):
        return repr(object.__getattribute__(self, '_data'))
    def __dir__(self):
        return list(object.__getattribute__(self, '_data').keys())
class _StreamColumnAccessor:
    def __init__(self, stream, col):
        object.__setattr__(self, '_stream', stream)
        object.__setattr__(self, '_col', col)
    def __iter__(self):
        _s = object.__getattribute__(self, '_stream')
        _c = object.__getattribute__(self, '_col')
        for row in _s:
            yield object.__getattribute__(row, '_data')[_c]
    def mean(self):
        total = 0.0
        count = 0
        for v in self:
            if v is not None and v == v:
                try:
                    total += float(v)
                    count += 1
                except (ValueError, TypeError):
                    pass
        return total / count if count > 0 else float('nan')
    def sum(self):
        total = 0.0
        for v in self:
            if v is not None and v == v:
                try:
                    total += float(v)
                except (ValueError, TypeError):
                    pass
        return total
    def min(self):
        result = None
        for v in self:
            if v is not None and v == v:
                try:
                    fv = float(v)
                    if result is None or fv < result:
                        result = fv
                except (ValueError, TypeError):
                    pass
        return result if result is not None else float('nan')
    def max(self):
        result = None
        for v in self:
            if v is not None and v == v:
                try:
                    fv = float(v)
                    if result is None or fv > result:
                        result = fv
                except (ValueError, TypeError):
                    pass
        return result if result is not None else float('nan')
    def value_counts(self):
        counts = {}
        for v in self:
            key = v if not (isinstance(v, float) and v != v) else None
            counts[key] = counts.get(key, 0) + 1
        return pd.Series(counts).sort_values(ascending=False)
    def __gt__(self, other):
        return _StreamFilter(object.__getattribute__(self, '_stream'), object.__getattribute__(self, '_col'), '>', other)
    def __ge__(self, other):
        return _StreamFilter(object.__getattribute__(self, '_stream'), object.__getattribute__(self, '_col'), '>=', other)
    def __lt__(self, other):
        return _StreamFilter(object.__getattribute__(self, '_stream'), object.__getattribute__(self, '_col'), '<', other)
    def __le__(self, other):
        return _StreamFilter(object.__getattribute__(self, '_stream'), object.__getattribute__(self, '_col'), '<=', other)
    def __eq__(self, other):
        return _StreamFilter(object.__getattribute__(self, '_stream'), object.__getattribute__(self, '_col'), '==', other)
    def __ne__(self, other):
        return _StreamFilter(object.__getattribute__(self, '_stream'), object.__getattribute__(self, '_col'), '!=', other)
class _StreamFilter:
    def __init__(self, stream, col, op, value):
        object.__setattr__(self, '_stream', stream)
        object.__setattr__(self, '_col', col)
        object.__setattr__(self, '_op', op)
        object.__setattr__(self, '_value', value)
    def _test(self, row_data):
        _col = object.__getattribute__(self, '_col')
        _op = object.__getattribute__(self, '_op')
        _val = object.__getattribute__(self, '_value')
        v = row_data.get(_col)
        if v is None or (isinstance(v, float) and v != v):
            return False
        try:
            fv = float(v)
            fval = float(_val)
            if _op == '>': return fv > fval
            if _op == '>=': return fv >= fval
            if _op == '<': return fv < fval
            if _op == '<=': return fv <= fval
        except (ValueError, TypeError):
            pass
        if _op == '==': return v == _val
        if _op == '!=': return v != _val
        return False
    def __and__(self, other):
        return _StreamCompositeFilter(self, other, 'and')
    def __or__(self, other):
        return _StreamCompositeFilter(self, other, 'or')
class _StreamCompositeFilter:
    def __init__(self, left, right, logic):
        object.__setattr__(self, '_left', left)
        object.__setattr__(self, '_right', right)
        object.__setattr__(self, '_logic', logic)
    def _test(self, row_data):
        _l = object.__getattribute__(self, '_left')._test(row_data)
        _r = object.__getattribute__(self, '_right')._test(row_data)
        if object.__getattribute__(self, '_logic') == 'and':
            return _l and _r
        return _l or _r
    def __and__(self, other):
        return _StreamCompositeFilter(self, other, 'and')
    def __or__(self, other):
        return _StreamCompositeFilter(self, other, 'or')
def _parse_stream_row(raw_line, cols):
    _row_vals = list(_csv.reader([raw_line]))[0]
    _data = {}
    for _i, _col in enumerate(cols):
        _v = _row_vals[_i] if _i < len(_row_vals) else ''
        if isinstance(_v, str) and _v.startswith('__IMG__:'):
            _v = _decode_img(_v)
        elif isinstance(_v, str) and _v.startswith('__DICT__:'):
            _v = _decode_dict(_v)
        elif isinstance(_v, str) and _v.startswith('__OBJ__:'):
            _v = _decode_obj(_v)
        else:
            try:
                _v = int(_v)
            except (ValueError, TypeError):
                try:
                    _v = float(_v)
                except (ValueError, TypeError):
                    pass
        _data[_col] = _v
    return _data
class _IntelliSEMStream:
    def __init__(self, name, columns, can_restream=False):
        object.__setattr__(self, '_name', name)
        object.__setattr__(self, '_columns', columns)
        object.__setattr__(self, '_can_restream', can_restream)
        object.__setattr__(self, '_consumed', False)
        object.__setattr__(self, '_pass_count', 0)
        object.__setattr__(self, '_cached_rows', None)
        object.__setattr__(self, '_cached_len', None)
    @property
    def columns(self):
        return list(object.__getattribute__(self, '_columns'))
    @property
    def name(self):
        return object.__getattribute__(self, '_name')
    def _request_restream(self):
        _name = object.__getattribute__(self, '_name')
        _can = object.__getattribute__(self, '_can_restream')
        if not _can:
            raise RuntimeError(f"Stream '{_name}' cannot be re-streamed. Use SetSource(Func<IEnumerable<T>>) on the .NET side for multi-pass support.")
        sys.stdout.write(f'__RESTREAM__:{_name}\n')
        sys.stdout.flush()
    def _raw_iter(self):
        _consumed = object.__getattribute__(self, '_consumed')
        _pass = object.__getattribute__(self, '_pass_count')
        if _consumed:
            self._request_restream()
        object.__setattr__(self, '_consumed', True)
        object.__setattr__(self, '_pass_count', _pass + 1)
        _cols = object.__getattribute__(self, '_columns')
        while True:
            _raw = sys.stdin.readline()
            if not _raw:
                break
            _raw = _raw.rstrip('\n')
            if _raw == '__STREAM_END__':
                break
            yield _parse_stream_row(_raw, _cols)
    def __iter__(self):
        _cached = object.__getattribute__(self, '_cached_rows')
        if _cached is not None:
            for row_data in _cached:
                yield _StreamRow(row_data)
            return
        for row_data in self._raw_iter():
            yield _StreamRow(row_data)
    def __len__(self):
        _cached_len = object.__getattribute__(self, '_cached_len')
        if _cached_len is not None:
            return _cached_len
        _cached = object.__getattribute__(self, '_cached_rows')
        if _cached is not None:
            return len(_cached)
        rows = list(self._raw_iter())
        object.__setattr__(self, '_cached_rows', rows)
        object.__setattr__(self, '_cached_len', len(rows))
        return len(rows)
    def __repr__(self):
        _n = object.__getattribute__(self, '_name')
        _c = object.__getattribute__(self, '_consumed')
        _cr = object.__getattribute__(self, '_can_restream')
        return f'IntelliSEMStream({_n}, columns={self.columns}, consumed={_c}, restream={_cr})'
    def __getattr__(self, name):
        _cols = object.__getattribute__(self, '_columns')
        if name in _cols:
            return _StreamColumnAccessor(self, name)
        raise AttributeError(f"Stream has no column '{name}'")
    def __getitem__(self, key):
        if isinstance(key, int):
            return self._get_by_index(key)
        if isinstance(key, slice):
            return self._get_by_slice(key)
        if isinstance(key, str):
            _cols = object.__getattribute__(self, '_columns')
            if key in _cols:
                return _StreamColumnAccessor(self, key)
            raise KeyError(f"Column '{key}' not found")
        if isinstance(key, list):
            return self._get_columns_as_df(key)
        if isinstance(key, (_StreamFilter, _StreamCompositeFilter)):
            return self._get_filtered(key)
        raise TypeError(f"Unsupported index type: {type(key)}")
    def _get_by_index(self, idx):
        if idx < 0:
            rows = list(self._raw_iter())
            object.__setattr__(self, '_cached_rows', rows)
            return _StreamRow(rows[idx])
        for i, row_data in enumerate(self._raw_iter()):
            if i == idx:
                return _StreamRow(row_data)
        raise IndexError(f"Index {idx} out of range")
    def _get_by_slice(self, s):
        start = s.start or 0
        stop = s.stop
        step = s.step or 1
        if start < 0 or (stop is not None and stop < 0):
            rows = list(self._raw_iter())
            object.__setattr__(self, '_cached_rows', rows)
            sliced = rows[s]
            df = pd.DataFrame(sliced)
            return _IntelliSEMDataset(df)
        collected = []
        for i, row_data in enumerate(self._raw_iter()):
            if stop is not None and i >= stop:
                break
            if i >= start and (i - start) % step == 0:
                collected.append(row_data)
        df = pd.DataFrame(collected)
        return _IntelliSEMDataset(df)
    def _get_columns_as_df(self, col_list):
        _cols = object.__getattribute__(self, '_columns')
        for c in col_list:
            if c not in _cols:
                raise KeyError(f"Column '{c}' not found. Available: {_cols}")
        rows = []
        for row_data in self._raw_iter():
            rows.append({c: row_data[c] for c in col_list})
        df = pd.DataFrame(rows, columns=col_list)
        return df
    def _get_filtered(self, filt):
        return _FilteredStream(self, filt)
    def head(self, n=5):
        return self[0:n]
    def tail(self, n=5):
        return self[-n:]
    def to_df(self):
        _cached = object.__getattribute__(self, '_cached_rows')
        if _cached is not None:
            return _IntelliSEMDataset(pd.DataFrame(_cached))
        rows = list(self._raw_iter())
        object.__setattr__(self, '_cached_rows', rows)
        return _IntelliSEMDataset(pd.DataFrame(rows))
    @property
    def df(self):
        return object.__getattribute__(self.to_df(), '_df')
    def describe(self):
        stats = {}
        _cols = object.__getattribute__(self, '_columns')
        counts = {c: 0 for c in _cols}
        sums = {c: 0.0 for c in _cols}
        sum2s = {c: 0.0 for c in _cols}
        mins = {c: None for c in _cols}
        maxs = {c: None for c in _cols}
        for row_data in self._raw_iter():
            for c in _cols:
                v = row_data.get(c)
                if v is None or (isinstance(v, float) and v != v):
                    continue
                try:
                    fv = float(v)
                except (ValueError, TypeError):
                    continue
                counts[c] += 1
                sums[c] += fv
                sum2s[c] += fv * fv
                if mins[c] is None or fv < mins[c]:
                    mins[c] = fv
                if maxs[c] is None or fv > maxs[c]:
                    maxs[c] = fv
        numeric_cols = [c for c in _cols if counts[c] > 0]
        result = {}
        for c in numeric_cols:
            n = counts[c]
            mean = sums[c] / n
            variance = (sum2s[c] / n) - (mean * mean)
            std = variance ** 0.5 if variance > 0 else 0.0
            result[c] = {
                'count': n,
                'mean': mean,
                'std': std,
                'min': mins[c],
                'max': maxs[c],
            }
        return pd.DataFrame(result).T[['count', 'mean', 'std', 'min', 'max']]
class _FilteredStream:
    def __init__(self, source, filt):
        object.__setattr__(self, '_source', source)
        object.__setattr__(self, '_filter', filt)
    def __iter__(self):
        _src = object.__getattribute__(self, '_source')
        _filt = object.__getattribute__(self, '_filter')
        for row in _src:
            row_data = object.__getattribute__(row, '_data')
            if _filt._test(row_data):
                yield _StreamRow(row_data)
    def __getitem__(self, key):
        if isinstance(key, int):
            for i, row in enumerate(self):
                if i == key:
                    return row
            raise IndexError(f"Index {key} out of range")
        if isinstance(key, slice):
            rows = []
            start = key.start or 0
            stop = key.stop
            step = key.step or 1
            for i, row in enumerate(self):
                if stop is not None and i >= stop:
                    break
                if i >= start and (i - start) % step == 0:
                    rows.append(object.__getattribute__(row, '_data'))
            return _IntelliSEMDataset(pd.DataFrame(rows))
        if isinstance(key, list):
            rows = []
            for row in self:
                rd = object.__getattribute__(row, '_data')
                rows.append({c: rd[c] for c in key})
            return pd.DataFrame(rows, columns=key)
        raise TypeError(f"Unsupported index type: {type(key)}")
    def __getattr__(self, name):
        _src = object.__getattribute__(self, '_source')
        _cols = object.__getattribute__(_src, '_columns')
        if name in _cols:
            return _StreamColumnAccessor(self, name)
        raise AttributeError(f"Filtered stream has no column '{name}'")
    @property
    def columns(self):
        _src = object.__getattribute__(self, '_source')
        return _src.columns
    def head(self, n=5):
        return self[0:n]
    def to_df(self):
        rows = [object.__getattribute__(row, '_data') for row in self]
        return _IntelliSEMDataset(pd.DataFrame(rows))
    @property
    def df(self):
        return object.__getattribute__(self.to_df(), '_df')
