# 数学宠物岛

C# 开发的离线安卓数学学习游戏。使用 .NET 10、.NET MAUI 和 Blazor Hybrid，实现已确认 HTML 原型的教学与养宠流程。界面与规则由 C# / Razor 实现，JavaScript 只处理网页验证版的本地存储和滚动。

## 当前版本

- 年级与学期选择，知识点的目标、讲解、分步例题、引导练习、独立练习、知识点闯关。
- 一至六年级各一个自编样例知识点，共18道练习题、18道配套相似题。每题包含答案、单位、分层提示、解题步骤、常见错误和时限。
- 每日练习、限时闯关、星级、首次通关奖励、每天首次完成奖励。
- 精确识别整数、小数和等值分数，支持全角数字；可以省略题目单位，填写单位时必须匹配。
- 错题自动保存，相似题独立答对后移除，使用提示仍有积分但不立即移除错题。
- 按年级、难度奖励积分，重复做对和使用提示奖励递减，答错不扣分。
- 一只主宠物：改名、喂食、玩耍、清洁/抚摸、成长阶段、学习成就。
- 商店、背包、食物、永久玩具、蝴蝶结穿戴和家园装饰切换。
- 安卓本地 JSON 存档，原子替换；异常存档保留并备份，界面提示，不会自动覆盖。卸载/清除应用数据会失去本地进度。

**内容边界：** 这不是完整的人教版小学题库。教材版本、逐册目录和知识点学期归属需要核对；目录中的上册是样例分组，下册暂未填充。单元/学期综合关、其他宠物和额外成长动画显示为待开放。每日练习目前是当前知识点3题，待题库扩充后再加入跨知识点复习计划。

## 安装到安卓设备

构建产物：`artifacts/mathpet-0.1.0.apk`（开发签名，包含程序集，可独立安装，不需要电脑或网络运行）。最低 Android 7.0 / API 24；包含 ARM64 和 x86_64，适合多数现代手机与对应模拟器，暂不包含仅支持32位ARM的旧设备。需要设备具有可用的 Android System WebView。

1. 将 APK 传到手机，点击安装。
2. 根据手机提示，允许文件管理器或浏览器“安装未知应用”。
3. 安装后打开“数学宠物岛”。也可以启用 USB 调试后使用 `adb install -r artifacts/mathpet-0.1.0.apk`。

此版本已验证共享 C# 界面和规则，并通过 APK 构建及签名检查；**尚未在安卓真机或模拟器上运行验证**。请检查首次启动、软键盘输入、返回操作、旋转屏幕、离线使用和关闭重开后的进度恢复。更新安装应保留同一个开发签名密钥；不同签名不能覆盖安装，卸载会删除本地数据。开发签名不用于应用商店正式发布。

## 开发与验证

项目结构：

| 项目 | 职责 |
| --- | --- |
| `src/MathPet.Core` | 题库、精确判分、练习会话、奖励、宠物和文件存储 |
| `src/MathPet.UI` | 安卓与网页验证版共享的 Razor 界面、样式和存档协调 |
| `src/MathPet.Android` | MAUI 安卓宿主、设备存储、应用图标 |
| `src/MathPet.Web` | 本机浏览器验证宿主；安卓应用不依赖它 |
| `tests/MathPet.Core.Tests` | 核心规则和实际文件存档测试 |
| `scripts/smoke-ui.cjs` | 浏览器端完整流程验证 |

本机需要 `global.json` 指定的 .NET SDK，以及 `dotnet workload install maui-android`。构建 APK 还需要 JDK 21、Android SDK platform 36、build-tools 36.0.0、platform-tools，并在正常 Android SDK 安装流程中接受相应许可。

已配置的云环境先执行：

```bash
source scripts/cloud-env.sh
```

运行测试和网页验证版：

```bash
dotnet test tests/MathPet.Core.Tests --locked-mode
dotnet run --project src/MathPet.Web --urls http://127.0.0.1:8766
```

在另一个终端运行界面验证：

```bash
npm ci
npx playwright install chromium
npm run test:ui
```

如果已经安装 Chromium，可使用 `CHROMIUM_PATH=/usr/bin/chromium npm run test:ui`。截图保存在 `artifacts/screenshots`。网页验证版存档在浏览器 localStorage，与安卓设备存档独立。

构建 APK：

```bash
export ANDROID_HOME=/your/android-sdk
export JAVA_HOME=/your/jdk-21
export MATHPET_KEY_DIR=/your/private/dev-keys
bash scripts/build-android.sh
```

云环境已在 `scripts/cloud-env.sh` 配置相关路径；生成的开发密钥放在仓库外，勿删除，否则后续 APK 无法原位更新已有安装。NuGet 配置要求包签名，依赖锁文件用于重复构建。`test` 和旧 `.vscode` 文件是原仓库保留的控制台示例，不是安卓项目入口。
