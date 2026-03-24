# snippet: Scatter Plot
from IntelliSEM import employees
import matplotlib.pyplot as plt

fig, ax = plt.subplots(figsize=(10, 6))
ax.scatter(employees.Salary, employees.PerformanceScore,
           alpha=0.6, edgecolors='black', linewidth=0.5)
ax.set_xlabel('Salary ($)')
ax.set_ylabel('Performance Score')
ax.set_title('Salary vs Performance Score')
plt.tight_layout()
plt.show()
