from DotNetData import customers, employees

# Access columns directly: customers.CreditLimit.mean()
# Access rows by index: customers[0].Name
# Slice datasets: customers[0:5]
# Use .df for full DataFrame: customers.df.describe()

print('=== Data Science Workbench ===')
print()

# Quick look at customers
print(f'Customers: {len(customers)} records')
print(f'First customer: {customers[0].Name}')
print(f'Average credit limit: ${customers.CreditLimit.mean():.2f}')
print()
print('=== Customer Summary ===')
print(customers.df.describe())
