import pandas as pd
import matplotlib.pyplot as plt
import seaborn as sns

sns.set(style="whitegrid")

df = pd.read_csv("telemetry.csv")

# =====================================================
# GRAPH 1 — Average class time distribution (DONUT)
# =====================================================

avg_class_time = df[["time_bowman","time_knight","time_berserker"]].mean()

# Remove classes never used (fix label overlap)
avg_class_time = avg_class_time[avg_class_time > 0]

fig1, ax1 = plt.subplots()

ax1.pie(
    avg_class_time,
    labels=avg_class_time.index.str.replace("time_", "").str.capitalize(),
    autopct="%1.1f%%",
    startangle=90,
    pctdistance=0.85
)

# Donut hole
centre_circle = plt.Circle((0,0),0.70,fc='white')
fig1.gca().add_artist(centre_circle)

ax1.set_title("Average Class Time Distribution Per Session")

# =====================================================
# GRAPH 2 — Time Played vs Loot %
# =====================================================

# =====================================================
# GRAPH 2 — Time Played vs Loot % (TIGHT ZOOM)
# =====================================================

fig2, ax2 = plt.subplots()

sns.regplot(
    data=df,
    x="time_played",
    y="loot_taken_pct",
    scatter_kws={"s":80},
    ax=ax2
)

ax2.set_title("Correlation: Time Played vs Loot Collected")
ax2.set_xlabel("Time Played (seconds)")
ax2.set_ylabel("Loot Collected (%)")

# ---- FIXED ZOOM (fit axes to data) ----
x_min, x_max = df["time_played"].min(), df["time_played"].max()
y_min, y_max = df["loot_taken_pct"].min(), df["loot_taken_pct"].max()

x_margin = (x_max - x_min) * 0.1
y_margin = (y_max - y_min) * 0.1

ax2.set_xlim(x_min - x_margin, x_max + x_margin)
ax2.set_ylim(y_min - y_margin, y_max + y_margin)

# Correlation value on graph
correlation = df["time_played"].corr(df["loot_taken_pct"])
ax2.text(
    0.05, 0.95,
    f"Pearson r = {correlation:.2f}",
    transform=ax2.transAxes,
    fontsize=12,
    verticalalignment='top'
)

# =====================================================
# GRAPH 3 — Class Time vs Enemies Killed %
# =====================================================

fig3, ax3 = plt.subplots()

classes = {
    "Bowman": "time_bowman",
    "Knight": "time_knight",
    "Berserker": "time_berserker"
}

for class_name, column in classes.items():
    
    # Skip class if never used
    if df[column].sum() == 0:
        continue

    # Scatter + regression
    sns.regplot(
        data=df,
        x=column,
        y="enemies_killed_pct",
        scatter_kws={"s":60},
        label=class_name,
        ax=ax3
    )

    # Calculate correlation
    if df[column].nunique() > 1:
        r = df[column].corr(df["enemies_killed_pct"])
        print(f"{class_name} correlation r = {r:.2f}")

ax3.set_title("Time Spent Per Class vs Enemies Killed %")
ax3.set_xlabel("Time Spent (seconds)")
ax3.set_ylabel("Enemies Killed (%)")
ax3.legend()



# =====================================================
# SHOW BOTH GRAPHS
# =====================================================
plt.show()
