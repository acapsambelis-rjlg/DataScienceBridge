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
class _DotNetStream:
    def __init__(self, name, columns):
        object.__setattr__(self, '_name', name)
        object.__setattr__(self, '_columns', columns)
        object.__setattr__(self, '_consumed', False)
    @property
    def columns(self):
        return list(object.__getattribute__(self, '_columns'))
    @property
    def name(self):
        return object.__getattribute__(self, '_name')
    def __repr__(self):
        _n = object.__getattribute__(self, '_name')
        _c = object.__getattribute__(self, '_consumed')
        return f'DotNetStream({_n}, columns={self.columns}, consumed={_c})'
    def __iter__(self):
        if object.__getattribute__(self, '_consumed'):
            raise RuntimeError('Stream has already been consumed. Re-register the data source for another pass.')
        object.__setattr__(self, '_consumed', True)
        _cols = object.__getattribute__(self, '_columns')
        while True:
            _raw = sys.stdin.readline()
            if not _raw:
                break
            _raw = _raw.rstrip('\n')
            if _raw == '__STREAM_END__':
                break
            _row_vals = list(_csv.reader([_raw]))[0]
            _data = {}
            for _i, _col in enumerate(_cols):
                _v = _row_vals[_i] if _i < len(_row_vals) else ''
                if isinstance(_v, str) and _v.startswith('__IMG__:'):
                    _v = _decode_img(_v)
                elif isinstance(_v, str) and _v.startswith('__DICT__:'):
                    _v = _decode_dict(_v)
                elif isinstance(_v, str) and _v.startswith('__OBJ__:'):
                    _v = _decode_obj(_v)
                _data[_col] = _v
            yield _StreamRow(_data)
