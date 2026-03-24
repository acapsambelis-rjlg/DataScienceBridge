# snippet: ML: Customer Segmentation (PCA)
from IntelliSEM import customers
from sklearn.decomposition import PCA
from sklearn.preprocessing import StandardScaler, LabelEncoder
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np

df = customers.df.copy()

tier_enc = LabelEncoder()
df['TierEncoded'] = tier_enc.fit_transform(df['Tier'])

feature_cols = ['Age', 'CreditLimit', 'TierEncoded', 'IsActive']
X = df[feature_cols].copy()
X['IsActive'] = X['IsActive'].astype(int)
X = X.dropna()

scaler = StandardScaler()
X_scaled = scaler.fit_transform(X)

pca = PCA()
X_pca = pca.fit_transform(X_scaled)

print('=== Customer Segmentation \u2014 PCA ===')
print()
print('Explained Variance Ratio:')
for i, var in enumerate(pca.explained_variance_ratio_):
    cumulative = sum(pca.explained_variance_ratio_[:i+1])
    bar = '#' * int(var * 50)
    print(f'  PC{i+1}: {var:.3f} (cumulative: {cumulative:.3f})  {bar}')
print()

print('Principal Component Loadings:')
loadings = pd.DataFrame(pca.components_.T, index=feature_cols,
                         columns=[f'PC{i+1}' for i in range(len(feature_cols))])
print(loadings.round(3))

fig, axes = plt.subplots(1, 3, figsize=(18, 5))

axes[0].bar(range(1, len(pca.explained_variance_ratio_) + 1),
            pca.explained_variance_ratio_, color='steelblue', alpha=0.8)
axes[0].plot(range(1, len(pca.explained_variance_ratio_) + 1),
             np.cumsum(pca.explained_variance_ratio_), 'ro-')
axes[0].set_xlabel('Principal Component')
axes[0].set_ylabel('Variance Explained')
axes[0].set_title('Scree Plot')
axes[0].set_xticks(range(1, len(feature_cols) + 1))

tiers = df.loc[X.index, 'Tier']
tier_names = sorted(tiers.unique())
colors = plt.cm.Set1(np.linspace(0, 1, len(tier_names)))
for i, tier in enumerate(tier_names):
    mask = tiers == tier
    axes[1].scatter(X_pca[mask, 0], X_pca[mask, 1],
                    c=[colors[i]], label=tier, alpha=0.5, edgecolors='black', linewidth=0.3)
axes[1].set_xlabel('PC1')
axes[1].set_ylabel('PC2')
axes[1].set_title('Customers in PCA Space (by Tier)')
axes[1].legend()

for i, feat in enumerate(feature_cols):
    axes[2].arrow(0, 0, pca.components_[0, i], pca.components_[1, i],
                  head_width=0.05, head_length=0.02, fc='steelblue', ec='steelblue')
    axes[2].text(pca.components_[0, i] * 1.15, pca.components_[1, i] * 1.15,
                 feat, fontsize=9, ha='center')
axes[2].set_xlabel('PC1')
axes[2].set_ylabel('PC2')
axes[2].set_title('Feature Loadings (Biplot)')
axes[2].axhline(y=0, color='gray', linestyle='--', linewidth=0.5)
axes[2].axvline(x=0, color='gray', linestyle='--', linewidth=0.5)

plt.tight_layout()
plt.show()
