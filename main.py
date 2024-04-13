import json
from collections import defaultdict
import matplotlib
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np

matplotlib.rcParams['font.sans-serif'] = ['KaiTi']

relic_data = None

with open('relic.json') as relic:
    relic_data = json.load(relic)

relic_groups = defaultdict(lambda: defaultdict(lambda: defaultdict(int)))

for relic in relic_data['relics']:
    relic_action_s = relic['add_num']
    relic_action = relic['action']
    relic_rarity = relic['relic_rarity']
    relic_name = relic['relic_name']

    relic_action_group = 'Add'
    if relic_action_s == -1:
        relic_action_group = 'Consume'
    
    relic_groups[relic_action_group][relic_rarity][relic_name] += 1

# with open('stat.txt', 'w') as stat:
#     for group_key, rarity_data in relic_groups.items():
#         stat.write(group_key)
#         for rarity, name_counts in rarity_data.items():
#             stat.write(f'稀有度: {rarity}')
#             for name, count in name_counts.items():
#                 stat.write(f'名字: {name}, 数量: {count}')

# with open('stat.json', 'w', encoding='utf-8') as stat:
#     json.dump(relic_groups, stat, ensure_ascii=False)

# with open('relics.json', 'w', encoding='utf-8') as relic:
#     json.dump(relic_data, relic, ensure_ascii=False)


consume_rarity_5: defaultdict[str, int] = relic_groups["Consume"][5]
add_rarity_5: defaultdict[str, int] = relic_groups["Add"][5]

relic_names: tuple[str] = tuple(add_rarity_5.keys())
consume_counts = [consume_rarity_5[relic] if relic in consume_rarity_5 else 0 for relic in relic_names]
add_counts = [add_rarity_5[relic_name] if relic_name in add_rarity_5 else 0 for relic_name in relic_names]

relic_category = tuple(set(relic_name.split('的')[0] for relic_name in relic_names))
consume_group_cat = defaultdict(int)
add_group_cat = defaultdict(int)
for category in relic_category:
    for name, count in consume_rarity_5.items():
        if name.startswith(category):
            consume_group_cat[category] += count
    for name, count in add_rarity_5.items():
        if name.startswith(category):
            add_group_cat[category] += count
consume_counts_cat = [consume_group_cat[cat] if cat in consume_group_cat else 0 for cat in relic_category]
add_counts_cat = [add_group_cat[cat] if cat in add_group_cat else 0 for cat in relic_category]

ca_counts = {
    '消耗': np.array(consume_counts),
    '获取': np.array(add_counts)
}

ca_counts_cat = {
    '消耗': np.array(consume_counts_cat),
    '获取': np.array(add_counts_cat)
}

width = 0.6
figure, axis = plt.subplots(1, 2)

bottom = np.zeros(len(relic_names))
for ca, ca_count in ca_counts.items():
    p = axis[0].barh(relic_names, ca_count, width, label=ca, left=bottom)
    bottom += ca_count

    axis[0].bar_label(p, label_type='center')

axis[0].set_title('遗器')
axis[0].legend()

bottom = np.zeros(len(relic_category))
for ca, ca_count in ca_counts_cat.items():
    p = axis[1].barh(relic_category, ca_count, width, label=ca, left=bottom)
    bottom += ca_count

    axis[1].bar_label(p, label_type='center')

axis[1].set_title('遗器（按大类）')
axis[1].legend()

plt.xticks(rotation=90)
plt.show()
# plt.savefig('my_plot.png', dpi=300)