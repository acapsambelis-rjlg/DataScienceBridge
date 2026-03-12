# snippet: Stream Data (Lazy)
from DotNetData import customer_stream

print('=== Streaming Data Demo ===')
print(f'Stream: {customer_stream}')
print(f'Columns: {customer_stream.columns}')
print()

count = 0
tier_counts = {}
total_age = 0

for row in customer_stream:
    count += 1
    tier = row.Tier
    tier_counts[tier] = tier_counts.get(tier, 0) + 1

    if count <= 3:
        print(f'Row {count}: {row.FirstName} {row.LastName} - {row.Tier}')

print(f'...')
print(f'Total rows streamed: {count}')
print()
print('Tier distribution:')
for tier, n in sorted(tier_counts.items()):
    print(f'  {tier}: {n}')
