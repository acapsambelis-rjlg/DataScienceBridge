# snippet: Time Series Plot
from DotNetData import customers
import pandas as pd
import matplotlib.pyplot as plt

df = customers.df.copy()
df['RegistrationDate'] = pd.to_datetime(df['RegistrationDate'])
monthly = df.set_index('RegistrationDate').resample('M').size()

fig, ax = plt.subplots(figsize=(12, 6))
ax.plot(monthly.index, monthly.values, linewidth=1.5, marker='o', markersize=3)
ax.set_xlabel('Date')
ax.set_ylabel('New Registrations')
ax.set_title('Customer Registrations Over Time')
plt.xticks(rotation=45)
plt.tight_layout()
plt.show()
