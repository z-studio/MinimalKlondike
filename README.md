# Minimal Klondike Solver

用 C# 实现的 **Klondike（纸牌接龙）** 求解器：在给定发牌与翻牌张数下，用最佳优先搜索（带启发式与闭集）寻找**步数意义下的较优解**；也支持用 **Green Felt** 等站点的种子复现牌局。

---

## 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## 构建与运行

```bash
cd MinimalKlondike   # 或你的克隆目录名
dotnet build Klondike.csproj -c Release
dotnet run --project Klondike.csproj -c Release -- [参数…]
```

可执行文件一般在 `bin/Release/net8.0/`（或 `bin/Debug/...`）下，名称随系统为 `Klondike` 或 `Klondike.exe`。

## 命令行用法

无参数时打印帮助。一般形式：

```text
Klondike [选项…] <最后一项：牌局输入>
```

| 选项 | 含义 |
|------|------|
| `-D #` | 每次从**库存**翻到**废牌**的张数（`1` 或 `3` 等），默认 `1`。 |
| `-M "…"` | 求解前先按走法串执行若干步（见下文「走法串」）。 |
| `-S #` | 搜索闭集最大结点数（默认 `50000000`）；越大越吃内存与时间。 |

**最后一项参数** 有两种用法（与 `Program.cs` 一致）：

1. **短数字串**（长度 &lt; 11）：当作 **Green Felt 无翻牌种子**，走 `ShuffleGreenFelt(seed)`。  
   - 例：`dotnet run --project Klondike.csproj -- 123`

2. **长牌串**：整副 52 张牌的编码（见下文「牌串格式」），走 `SetDeal`。  
   - 例：见下方「完整牌串 + 前缀走法」示例。

程序内部对求解调用大致为：`Board.Solve(maxMoves: 250, maxRounds: 15, maxNodes: -S)`，并在控制台打印初始盘面、走法输出与结果统计。

### 关卡生成模式（`--generate`）

用于按规则**批量随机发牌 → 求解 → 筛选 → 追加写入**符合要求的**数字牌串**（每行一局，可供 `SetDeal` 使用）。

- **入库条件**：仅当 `Solve` 返回 `Solved` 或 `Minimal` 的局才会写入；未证可解或 `Unknown`/`Impossible` 一律丢弃。
- **步数**：与引擎一致，**一步 = 一次 `MakeMove`**；「首次翻盖牌」指七列**盖牌张数**（各列 `First` 之和）**首次减少**的那一步（不把单纯废牌接桌当成盖牌翻开）。
- **盖牌 A / 2 / K 筛选（分组）**：每种点数独立。对盖牌中的该点数，**盖牌深度**= `first - j - 1`（该牌上方还有几张盖牌）。须配置**张数**筛（如 `--filter-key-ace-cover-count` / YAML `keyAceCoverCount`）该组才生效；可选再配**深度**筛，仅当深度落入 `(L,R]` 的牌才计入张数；不配深度筛时，该点数在盖牌里的**全部**张都计入张数。例：深度在 `(0,2]` 的盖牌 A 有 `(2,4]` 张 → `keyAceCoverDepth: "0,2"` 且 `keyAceCoverCount: "2,4"`。
- **区间筛选**：`(L,R]` 左开右闭；`L==R` 表示等于 `L`。详见 `LevelGenerateRunner --help`。

示例：

```bash
dotnet run --project Klondike.csproj -- --generate --attempts 200 --out ./levels.txt -D 1 \
  --filter-first-reveal 0,8 --filter-solve-moves 0,400
```

实现位于 `LevelGeneration/`；`Board` 的关卡用 API 在 `Entities/Board.LevelGeneration.cs`（与 `Board.cs` **分部类**同一名称，便于区分）。`Program` 的 `--generate` 入口在 `Program.LevelGeneration.cs`。

---

## 牌堆与走法里用的字母

走法串里用**单个字母**表示源/目标摞（与 `Move` 的 `char` 构造一致）：

| 字母 | 含义 |
|------|------|
| **A** | 废牌堆（Waste） |
| **B** | 梅花回收（Foundation ♣） |
| **C** | 方块回收（Foundation ♦） |
| **D** | 黑桃回收（Foundation ♠） |
| **E** | 红桃回收（Foundation ♥） |
| **F–L** | 桌面七列（Tableau 1–7） |

---

## 走法串（`-M` 与输出）

- 一般步：`XY` — `X` 源摞、`Y` 目标摞（如 `AE` 表示从废牌 A 收到红桃回收 E）。  
- **`@`**：表示**翻库存**（每次消耗与 `-D` 相关的若干张，具体与引擎内部 `Move.Count` 一致）；连续多个 `@` 表示多次翻牌。  
- 程序结束时会打印 `MovesMadeOutput`，格式与此相同，便于复制复现。

---

## 牌串格式（整副 52 张）

支持两种（由 `Board.SetDeal` 根据字符串判断）：

### 1）数字式 `RRS`（无空格，连续 156 个字符）

每张牌 **3 个字符**：

- **`RR`**：点数，`01`–`13`（`01`=A，`11`=J，`12`=Q，`13`=K）。  
- **`S`**：花色，**`1`–`4`**，对应 **梅花、方块、红桃、黑桃**（与源码中数字编码一致，不是按字母 C/D/H/S 顺序）。

牌在字符串中的**出现顺序**即发到桌面与库存的顺序（三角发七列，再发库存），与下面示意图一致：

```text
 A        B  C  D  E   （废牌/回收在求解器里有单独槽位，此图仅表示牌串读入顺序与桌面列对应关系）

 F  G  H  I  J  K  L
01 02 04 07 11 16 22
   03 05 08 12 17 23
      06 09 13 18 24
         10 14 19 25
            15 20 26
               21 27
                  28

52-29：库存（从牌串先后依次压入，最后一张牌为库存顶）
```

### 2）字母式（带空格）

若第 3 个字符为**空格**，则按 **`点数字母` + `花色字母` + 空格** 重复 52 次解析，例如 `A`+`H`+空格 表示红心 A。花色字母：`C/D/S/H`（梅花/方块/黑桃/红桃）。

---

## 示例（便于对照、可直接复制）

**数字牌串**（52×3=156 字符）：

```text
072103023042094134111092051034044074114052123011083122012131091082124064014093033112071104132053133102084041013073063031061043081054113062024021101022032121
```

示意（`+` 表示该列已翻开可见；具体以程序 `ToString()` 为准）：

```text
  A        B  C  D  E

  F  G  H  I  J  K  L
+7D TH 2H 4D 9S KS JC
   +9D 5C 3S 4S 7S JS
      +5D QH AC 8H QD
         +AD KC 9C 8D
            +QS 6S AS
               +9H 3H
                  +JD

 7C TS KD 5H KH TD 8S
 4C AH 7H 6H 3C 6C 4H
 8C 5S JH 6D 2S 2C TC
 2D 3D QC
```

**带前缀走法**（`-D 1` 时的一例）：

```text
Klondike.exe -D 1 -M "HE KE @@@@AD GD LJ @@AH @@AJ GJ @@@@AG @AB" 081054022072134033082024052064053012061013042093084124092122062031083121113023043074051114091014103044131063041102101133011111071073034123104112021132032094
```

---

## 求解结果 `ESolveResult`（简要）

| 值 | 含义（结合 `Board.Solve` 实现） |
|----|--------------------------------|
| `Solved` | 在限制内找到**完整收齐 52 张**的解（结点用尽时也可能标为已解，视搜索是否跑满）。 |
| `Minimal` | 在**未提前终止**且搜索跑完闭集的前提下，可理解为在所用模型下给出了更「紧」的结论（见代码分支）。 |
| `Impossible` | 在结点预算内可判定**无解**（与 `maxNodes`、状态空间有关）。 |
| `Unknown` | 未证完：可能超时/结点用尽，或尚未判定有解无解。 |

控制台还会输出：`Foundation`（已收张数）、`Moves`（折算后的步数度量）、`Rounds`（库存翻完轮数）、`States`（访问结点数）等。

---

## 代码结构（便于阅读）

| 目录/文件 | 作用 |
|-----------|------|
| `Program.cs` | 命令行入口、解析参数、调用 `Board.Solve`（`partial`）。 |
| `LevelGeneration/` | 关卡指标、区间筛选、批量生成与写文件。 |
| `Entities/Board.cs` | 牌局规则、走法、搜索主循环（`partial`，关卡 API 见 `Board.LevelGeneration.cs`）。 |
| `Entities/Board.LevelGeneration.cs` | `Board` 分部：关卡用 `RecordedMoves` / `GetPile`。 |
| `Program.LevelGeneration.cs` | `Program` 分部：`--generate` 与帮助片段。 |
| `Entities/Move.cs`、`Card.cs`、`Pile.cs` | 走法编码、牌与牌摞。 |
| `Entities/TalonHelper.cs` | 库存/废牌相关合法走法展开。 |
| `Entities/State*.cs`、`MoveNode.cs` | 闭集键与搜索路径回溯。 |
| `Collections/Heap.cs`、`HashMap.cs` | 开集堆与闭集哈希表。 |
| `KlondikeTest/` | xUnit 测试（需单独 `dotnet test` 该工程）。 |

---

## 测试

```bash
dotnet test KlondikeTest/KlondikeTest.csproj
```

---

## 许可

见仓库根目录 `LICENSE`。
