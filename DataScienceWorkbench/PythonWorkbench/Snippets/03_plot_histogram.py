# snippet: Plot Histogram
from IntelliSEM import employees
import matplotlib.pyplot as plt

fig, ax = plt.subplots(figsize=(10, 6))
ax.hist(employees.Salary, bins=30, edgecolor='black', alpha=0.7)
ax.set_xlabel('Salary ($)')
ax.set_ylabel('Count')
ax.set_title('Employee Salary Distribution')
plt.tight_layout()
plt.show()
