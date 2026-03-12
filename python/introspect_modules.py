import sys
import json
import importlib
import inspect

MAX_CLASSES = 50
MAX_DOCS = 200

PROMINENT_CLASSES = {
    "pandas": ["DataFrame", "Series", "Index", "Categorical", "Timestamp",
               "Timedelta", "DatetimeIndex", "MultiIndex", "Grouper"],
    "numpy": ["ndarray", "matrix", "dtype", "random", "linalg", "fft"],
    "sklearn": [],
    "matplotlib": [],
    "matplotlib.pyplot": ["Figure", "Axes"],
}

def _get_signature(obj, name):
    try:
        sig = inspect.signature(obj)
        return name + str(sig)
    except (ValueError, TypeError):
        return name + "(...)"

def _get_short_doc(obj, max_lines=6):
    doc = inspect.getdoc(obj)
    if not doc:
        return ""
    lines = doc.strip().split('\n')
    kept = []
    for line in lines[:max_lines]:
        kept.append(line)
    return '\n'.join(kept)

def introspect_module(module_name):
    result = {
        "functions": [], "classes": {}, "constants": [], "submodules": [],
        "function_docs": {}, "class_docs": {}
    }
    try:
        mod = importlib.import_module(module_name)
    except Exception:
        return None

    prominent = PROMINENT_CLASSES.get(module_name)
    class_count = 0
    doc_count = 0

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

            if doc_count < MAX_DOCS:
                doc_count += 1
                init = getattr(obj, '__init__', None)
                if init is not None and init is not object.__init__:
                    sig = _get_signature(init, name)
                    sig = sig.replace("(self, ", "(").replace("(self)", "()")
                else:
                    sig = name + "(...)"
                doc = _get_short_doc(obj)
                result["class_docs"][name] = {"sig": sig, "doc": doc}

                for mname in sorted(dir(obj)):
                    if mname.startswith('_'):
                        continue
                    if doc_count >= MAX_DOCS:
                        break
                    try:
                        mobj = getattr(obj, mname)
                    except Exception:
                        continue
                    if callable(mobj):
                        doc_count += 1
                        msig = _get_signature(mobj, mname)
                        msig = msig.replace("(self, ", "(").replace("(self)", "()")
                        mdoc = _get_short_doc(mobj, max_lines=3)
                        key = name + "." + mname
                        result["function_docs"][key] = {"sig": msig, "doc": mdoc}

        elif inspect.isfunction(obj) or inspect.isbuiltin(obj) or (callable(obj) and not inspect.ismodule(obj)):
            result["functions"].append(name)
            if doc_count < MAX_DOCS:
                doc_count += 1
                sig = _get_signature(obj, name)
                doc = _get_short_doc(obj)
                result["function_docs"][name] = {"sig": sig, "doc": doc}
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
