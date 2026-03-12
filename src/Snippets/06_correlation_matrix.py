# snippet: Correlation Matrix
from DotNetData import employees
import matplotlib.pyplot as plt

numeric_cols = employees.df.select_dtypes(include='number')
corr = numeric_cols.corr()
print('=== Correlation Matrix ===')
print(corr.round(3))

fig, ax = plt.subplots(figsize=(10, 8))
im = ax.imshow(corr, cmap='coolwarm', vmin=-1, vmax=1)
ax.set_xticks(range(len(corr.columns)))
ax.set_yticks(range(len(corr.columns)))
ax.set_xticklabels(corr.columns, rotation=45, ha='right')
ax.set_yticklabels(corr.columns)
plt.colorbar(im)
ax.set_title('Employee Data Correlation Matrix')
plt.tight_layout()
plt.show()
