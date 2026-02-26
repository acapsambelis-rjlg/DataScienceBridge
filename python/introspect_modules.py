import sys
import json
import importlib
import inspect

MAX_CLASSES = 50

PROMINENT_CLASSES = {
    "pandas": ["DataFrame", "Series", "Index", "Categorical", "Timestamp",
               "Timedelta", "DatetimeIndex", "MultiIndex", "Grouper"],
    "numpy": ["ndarray", "matrix", "dtype", "random", "linalg", "fft"],
    "sklearn": [],
    "matplotlib": [],
    "matplotlib.pyplot": ["Figure", "Axes"],
}

def introspect_module(module_name):
    result = {"functions": [], "classes": {}, "constants": [], "submodules": []}
    try:
        mod = importlib.import_module(module_name)
    except Exception:
        return None

    prominent = PROMINENT_CLASSES.get(module_name)
    class_count = 0

    for name in sorted(dir(mod)):
        if name.startswith('_'):
            continue
        try:
            obj = getattr(mod, name)
        except Exception:
            continue

        if inspect.ismodule(obj):
            result["submodules"].append(name)
        elif inspect.isclass(obj):
            if prominent is not None and len(prominent) > 0 and name not in prominent:
                continue
            if class_count >= MAX_CLASSES:
                continue
            class_count += 1
            members = []
            for mname in sorted(dir(obj)):
                if mname.startswith('_'):
                    continue
                try:
                    mobj = getattr(obj, mname)
                except Exception:
                    continue
                if callable(mobj):
                    members.append(mname + "()")
                else:
                    members.append(mname)
            result["classes"][name] = members
        elif inspect.isfunction(obj) or inspect.isbuiltin(obj) or (callable(obj) and not inspect.ismodule(obj)):
            result["functions"].append(name)
        else:
            result["constants"].append(name)

    return result

def main():
    modules = sys.argv[1:]
    if not modules:
        sys.exit(0)

    output = {}
    for mod_name in modules:
        data = introspect_module(mod_name)
        if data is not None:
            output[mod_name] = data

    print("__INTROSPECT_START__")
    print(json.dumps(output))
    print("__INTROSPECT_END__")

if __name__ == "__main__":
    main()
