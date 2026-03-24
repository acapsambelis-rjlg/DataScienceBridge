# snippet: Basic Statistics
from IntelliSEM import customers

print('=== Descriptive Statistics ===')
print(customers.df.describe())
print()
print('=== Data Types ===')
print(customers.df.dtypes)
print()
print('=== Missing Values ===')
print(customers.df.isnull().sum())
