# snippet: ML: Employee Clustering (K-Means)
from DotNetData import employees
  from sklearn.cluster import KMeans
  from sklearn.preprocessing import StandardScaler
  import matplotlib.pyplot as plt
  import pandas as pd
  import numpy as np

  df = employees.df.copy()

  cluster_features = ['Salary', 'PerformanceScore', 'YearsEmployed']
  X = df[cluster_features].dropna()
  scaler = StandardScaler()
  X_scaled = scaler.fit_transform(X)

  inertias = []
  K_range = range(2, 9)
  for k in K_range:
      km = KMeans(n_clusters=k, random_state=42, n_init=10)
      km.fit(X_scaled)
      inertias.append(km.inertia_)

  n_clusters = 4
  kmeans = KMeans(n_clusters=n_clusters, random_state=42, n_init=10)
  df.loc[X.index, 'Cluster'] = kmeans.fit_predict(X_scaled)

  print('=== Employee Clustering — K-Means ===')
  print(f'Number of clusters: {n_clusters}')
  print()
  for c in range(n_clusters):
      subset = df[df['Cluster'] == c]
      print(f'Cluster {c} ({len(subset)} employees):')
      print(f'  Avg Salary:      ${subset["Salary"].mean():,.0f}')
      print(f'  Avg Performance:  {subset["PerformanceScore"].mean():.2f}')
      print(f'  Avg Tenure:       {subset["YearsEmployed"].mean():.1f} years')
      print(f'  Top Departments:  {", ".join(subset["Department"].value_counts().head(3).index)}')
      print()

  fig, axes = plt.subplots(1, 3, figsize=(18, 5))

  axes[0].plot(list(K_range), inertias, 'bo-')
  axes[0].axvline(x=n_clusters, color='r', linestyle='--', alpha=0.7)
  axes[0].set_xlabel('Number of Clusters (k)')
  axes[0].set_ylabel('Inertia')
  axes[0].set_title('Elbow Method')

  colors = plt.cm.Set2(np.linspace(0, 1, n_clusters))
  for c in range(n_clusters):
      mask = df['Cluster'] == c
      axes[1].scatter(df.loc[mask, 'Salary'], df.loc[mask, 'PerformanceScore'],
                      c=[colors[c]], label=f'Cluster {c}', alpha=0.6, edgecolors='black', linewidth=0.3)
  axes[1].set_xlabel('Salary ($)')
  axes[1].set_ylabel('Performance Score')
  axes[1].set_title('Clusters: Salary vs Performance')
  axes[1].legend()

  for c in range(n_clusters):
      mask = df['Cluster'] == c
      axes[2].scatter(df.loc[mask, 'YearsEmployed'], df.loc[mask, 'Salary'],
                      c=[colors[c]], label=f'Cluster {c}', alpha=0.6, edgecolors='black', linewidth=0.3)
  axes[2].set_xlabel('Years Employed')
  axes[2].set_ylabel('Salary ($)')
  axes[2].set_title('Clusters: Tenure vs Salary')
  axes[2].legend()

  plt.tight_layout()
  plt.show()
  