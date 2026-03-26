import sys, io, base64, json as _json, pandas as pd
import numpy as np
from PIL import Image as _PILImage
import csv as _csv
_csv.field_size_limit(2**31 - 1)
def _decode_img(s):
    if s is None or (isinstance(s, float) and s != s): return None
    if not isinstance(s, str) or s == '': return None
    if not s.startswith('__IMG__:'): return s
    try:
        b = base64.b64decode(s[7:])
        return _PILImage.open(io.BytesIO(b))
    except Exception:
        return None
def _decode_img_columns(df):
    for col in df.columns:
        first = df[col].dropna().iloc[0] if len(df[col].dropna()) > 0 else None
        if isinstance(first, str) and first.startswith('__IMG__:'):
            df[col] = df[col].apply(_decode_img)
    return df
def _decode_dict(s):
    if s is None or (isinstance(s, float) and s != s): return None
    if not isinstance(s, str) or s == '': return None
    if not s.startswith('__DICT__:'): return s
    try:
        d = _json.loads(s[9:])
        return _NestedObject(d) if isinstance(d, dict) else d
    except Exception:
        return s
def _decode_dict_columns(df):
    for col in df.columns:
        first = df[col].dropna().iloc[0] if len(df[col].dropna()) > 0 else None
        if isinstance(first, str) and first.startswith('__DICT__:'):
            df[col] = df[col].apply(_decode_dict)
    return df
class _NestedObject:
    def __init__(self, data):
        object.__setattr__(self, '_data', data if isinstance(data, dict) else {})
    @staticmethod
    def _wrap(v):
        return _NestedObject(v) if isinstance(v, dict) else v
    def __getattr__(self, name):
        _data = object.__getattribute__(self, '_data')
        if name in _data:
            return _NestedObject._wrap(_data[name])
        raise AttributeError(f"Object has no property '{name}'")
    def __getitem__(self, key):
        _data = object.__getattribute__(self, '_data')
        return _NestedObject._wrap(_data[key])
    def __contains__(self, key):
        return key in object.__getattribute__(self, '_data')
    def __len__(self):
        return len(object.__getattribute__(self, '_data'))
    def __iter__(self):
        return iter(object.__getattribute__(self, '_data'))
    def keys(self):
        return object.__getattribute__(self, '_data').keys()
    def values(self):
        return [_NestedObject._wrap(v) for v in object.__getattribute__(self, '_data').values()]
    def items(self):
        return [(k, _NestedObject._wrap(v)) for k, v in object.__getattribute__(self, '_data').items()]
    def get(self, key, default=None):
        _data = object.__getattribute__(self, '_data')
        return _NestedObject._wrap(_data[key]) if key in _data else default
    def __repr__(self):
        return repr(object.__getattribute__(self, '_data'))
    def __dir__(self):
        return list(object.__getattribute__(self, '_data').keys())
    def __eq__(self, other):
        if isinstance(other, _NestedObject):
            return object.__getattribute__(self, '_data') == object.__getattribute__(other, '_data')
        return NotImplemented
    def __hash__(self):
        return id(self)
def _decode_obj(s):
    if s is None or (isinstance(s, float) and s != s): return None
    if not isinstance(s, str) or s == '': return None
    if not s.startswith('__OBJ__:'): return s
    try:
        return _NestedObject(_json.loads(s[8:]))
    except Exception:
        return s
def _decode_obj_columns(df):
    for col in df.columns:
        first = df[col].dropna().iloc[0] if len(df[col].dropna()) > 0 else None
        if isinstance(first, str) and first.startswith('__OBJ__:'):
            df[col] = df[col].apply(_decode_obj)
    return df
class _DatasetRow:
    def __init__(self, series):
        object.__setattr__(self, '_s', series)
    def __getattr__(self, name):
        _s = object.__getattribute__(self, '_s')
        if name in _s.index:
            return _s[name]
        raise AttributeError(f"Row has no field '{name}'")
    def __repr__(self):
        return repr(object.__getattribute__(self, '_s'))
    def __dir__(self):
        _s = object.__getattribute__(self, '_s')
        return list(_s.index)
class _IntelliSEMDataset:
    def __init__(self, df):
        object.__setattr__(self, '_df', df)
    def __getattr__(self, name):
        _df = object.__getattribute__(self, '_df')
        if name in _df.columns:
            return _df[name]
        return getattr(_df, name)
    def __repr__(self):
        return repr(object.__getattribute__(self, '_df'))
    def __len__(self):
        return len(object.__getattribute__(self, '_df'))
    def __getitem__(self, key):
        _df = object.__getattribute__(self, '_df')
        if isinstance(key, (int, slice)):
            result = _df.iloc[key]
            if isinstance(result, pd.Series):
                return _DatasetRow(result)
            return _IntelliSEMDataset(result.reset_index(drop=True))
        return _df[key]
    def __iter__(self):
        _df = object.__getattribute__(self, '_df')
        for i in range(len(_df)):
            yield _DatasetRow(_df.iloc[i])
    @property
    def df(self):
        return object.__getattribute__(self, '_df')
