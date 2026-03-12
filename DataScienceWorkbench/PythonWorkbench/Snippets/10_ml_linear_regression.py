# snippet: ML: Salary Prediction (Linear Regression)
# separator: before
from DotNetData import employees
  from sklearn.linear_model import LinearRegression
  from sklearn.model_selection import train_test_split
  from sklearn.metrics import mean_absolute_error, r2_score
  import matplotlib.pyplot as plt
  import pandas as pd
  import numpy as np

  # Salary is generated from: DeptBase + YearsEmployed*$2800 + TitleLevel*12% + noise
  # This model should recover those relationships with high R²

  df = employees.df.copy()

  features = pd.get_dummies(df[['YearsEmployed', 'PerformanceScore', 'IsRemote', 'Department']], drop_first=True)
  target = df['Salary']

  X_train, X_test, y_train, y_test = train_test_split(features, target, test_size=0.25, random_state=42)

  model = LinearRegression()
  model.fit(X_train, y_train)
  y_pred = model.predict(X_test)

  print('=== Salary Prediction — Linear Regression ===')
  print()
  print('Known data correlations (built into the dataset):')
  print('  Salary = DepartmentBase + YearsEmployed * ~$2,800 + TitleLevel * 12%')
  print('  Engineering base ~$115K, Support base ~$60K')
  print()
  print(f'R² Score:           {r2_score(y_test, y_pred):.4f}')
  print(f'Mean Absolute Error: ${mean_absolute_error(y_test, y_pred):,.2f}')
  print()

  coefs = pd.Series(model.coef_, index=features.columns).sort_values()
  print('Recovered Coefficients (compare to known structure):')
  tenure_coef = coefs.get('YearsEmployed', 0)
  print(f'  YearsEmployed coefficient: ${tenure_coef:+,.0f}/yr  (expected ~$2,800/yr)')
  print()
  print('All feature coefficients:')
  for name, val in coefs.items():
      print(f'  {name:30s} {val:+,.2f}')

  # Show the actual correlations in the raw data
  print()
  print('Raw correlation matrix (key columns):')
  corr_cols = ['Salary', 'YearsEmployed', 'PerformanceScore', 'IsRemote']
  print(df[corr_cols].corr().round(3).to_string())

  fig, axes = plt.subplots(1, 3, figsize=(18, 5))

  axes[0].scatter(df['YearsEmployed'], df['Salary'], alpha=0.4, c='steelblue', edgecolors='black', linewidth=0.3)
  z = np.polyfit(df['YearsEmployed'], df['Salary'], 1)
  x_line = np.linspace(df['YearsEmployed'].min(), df['YearsEmployed'].max(), 100)
  axes[0].plot(x_line, np.polyval(z, x_line), 'r-', linewidth=2, label=f'slope=${z[0]:,.0f}/yr')
  axes[0].set_xlabel('Years Employed')
  axes[0].set_ylabel('Salary ($)')
  axes[0].set_title('Tenure → Salary (Known Correlation)')
  axes[0].legend()

  axes[1].scatter(y_test, y_pred, alpha=0.5, edgecolors='black', linewidth=0.5)
  mn, mx = min(y_test.min(), y_pred.min()), max(y_test.max(), y_pred.max())
  axes[1].plot([mn, mx], [mn, mx], 'r--', linewidth=1)
  axes[1].set_xlabel('Actual Salary ($)')
  axes[1].set_ylabel('Predicted Salary ($)')
  axes[1].set_title(f'Actual vs Predicted (R²={r2_score(y_test, y_pred):.3f})')

  top_coefs = coefs.abs().nlargest(10).index
  coefs[top_coefs].sort_values().plot.barh(ax=axes[2], color=['#d9534f' if v < 0 else '#5cb85c' for v in coefs[top_coefs].sort_values()])
  axes[2].set_xlabel('Coefficient Value')
  axes[2].set_title('Top 10 Feature Coefficients')

  plt.tight_layout()
  plt.show()
  