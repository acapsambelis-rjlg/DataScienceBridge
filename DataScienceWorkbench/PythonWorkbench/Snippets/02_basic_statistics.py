# snippet: Basic Statistics
from DotNetData import customers

print('=== Descriptive Statistics ===')
print(customers.df.describe())
print()
print('=== Data Types ===')
print(customers.df.dtypes)
print()
print('=== Missing Values ===')
print(customers.df.isnull().sum())
