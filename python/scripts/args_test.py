import sys
print("Arguments received:", sys.argv[1:])
print("Number of args:", len(sys.argv) - 1)
for i, arg in enumerate(sys.argv[1:]):
    print(f"  arg[{i}]: {arg}")
