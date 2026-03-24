# snippet: List Datasets
from IntelliSEM import customers, employees, customer_stream

for name, ds in [('customers', customers), ('employees', employees)]:
    print(f'  {name}: {len(ds)} rows, {len(ds.df.columns)} columns')

print(f'  customer_stream: streaming, columns={customer_stream.columns}')
