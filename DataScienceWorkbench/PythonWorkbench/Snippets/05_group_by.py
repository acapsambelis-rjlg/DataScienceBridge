# snippet: Group By Analysis
from DotNetData import employees

print('=== Average Salary by Department ===')
group = employees.df.groupby('Department').agg(
    Count=('Id', 'count'),
    Avg_Salary=('Salary', 'mean'),
    Avg_Performance=('PerformanceScore', 'mean')
).round(2)
print(group.sort_values('Avg_Salary', ascending=False))
