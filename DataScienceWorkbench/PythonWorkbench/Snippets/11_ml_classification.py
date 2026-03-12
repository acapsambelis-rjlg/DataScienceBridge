# snippet: ML: Department Classifier (Random Forest)
from DotNetData import employees
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, confusion_matrix
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np

df = employees.df.copy()

print('=== Department Classification \u2014 Random Forest ===')
print()
print('Department salary ranges (the key signal):')
dept_stats = df.groupby('Department')['Salary'].agg(['mean', 'std']).sort_values('mean', ascending=False)
for dept, row in dept_stats.iterrows():
    remote_pct = df[df['Department'] == dept]['IsRemote'].mean() * 100
    print(f'  {dept:15s}  avg ${row["mean"]:>9,.0f} \u00b1 ${row["std"]:>7,.0f}   remote: {remote_pct:.0f}%')
print()

feature_cols = ['Salary', 'PerformanceScore', 'YearsEmployed', 'IsRemote']
X = df[feature_cols].copy()
X['IsRemote'] = X['IsRemote'].astype(int)
y = df['Department']

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.25, random_state=42, stratify=y)

model = RandomForestClassifier(n_estimators=100, random_state=42)
model.fit(X_train, y_train)
y_pred = model.predict(X_test)

print(f'Accuracy: {model.score(X_test, y_test):.2%}')
print()
print(classification_report(y_test, y_pred, zero_division=0))

importances = pd.Series(model.feature_importances_, index=feature_cols).sort_values()
print('Feature importance (Salary dominates because each dept has a unique base):')
for feat, imp in importances.sort_values(ascending=False).items():
    print(f'  {feat:20s} {imp:.3f}')

fig, axes = plt.subplots(1, 3, figsize=(18, 5))

importances.plot.barh(ax=axes[0], color='steelblue')
axes[0].set_xlabel('Importance')
axes[0].set_title('Feature Importance')

for dept in sorted(df['Department'].unique()):
    mask = df['Department'] == dept
    axes[1].scatter(df.loc[mask, 'Salary'], df.loc[mask, 'IsRemote'].astype(int) + np.random.uniform(-0.15, 0.15, mask.sum()),
                    alpha=0.5, label=dept, s=20)
axes[1].set_xlabel('Salary ($)')
axes[1].set_ylabel('Is Remote (jittered)')
axes[1].set_title('Dept Separation by Salary & Remote')
axes[1].legend(fontsize=6, ncol=2, loc='center right')

labels = sorted(y.unique())
cm = confusion_matrix(y_test, y_pred, labels=labels)
im = axes[2].imshow(cm, cmap='Blues')
axes[2].set_xticks(range(len(labels)))
axes[2].set_yticks(range(len(labels)))
axes[2].set_xticklabels(labels, rotation=45, ha='right', fontsize=7)
axes[2].set_yticklabels(labels, fontsize=7)
axes[2].set_xlabel('Predicted')
axes[2].set_ylabel('Actual')
axes[2].set_title('Confusion Matrix')
for i in range(len(labels)):
    for j in range(len(labels)):
        axes[2].text(j, i, str(cm[i, j]), ha='center', va='center',
                     color='white' if cm[i, j] > cm.max() / 2 else 'black', fontsize=7)

plt.tight_layout()
plt.show()
