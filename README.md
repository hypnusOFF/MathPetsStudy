# 数学宠物岛

C# 开发的离线安卓数学学习游戏。使用 .NET 10、.NET MAUI 和 Blazor Hybrid，实现已确认 HTML 原型的教学与养宠流程。界面与规则由 C# / Razor 实现，JavaScript 只处理网页验证版的本地存储和滚动。

## 许可与使用范围

本项目采用[专有许可](LICENSE)，源码可查看，**不采用开源许可证**。允许个人免费使用官方 APK，供本人及家庭非商业学习和体验；可以分享官方下载链接。源码可为个人查看而下载或克隆，修改、编译、资源复用、再分发及商业用途需另获权利人 `hypnusOFF` 的书面授权。GitHub 服务条款允许的平台查看与 Fork 权限予以保留；第三方依赖及外部课程内容仍遵循各自许可。完整适用范围及例外以 [LICENSE](LICENSE) 为准。

下文的开发、构建和资源制作说明面向项目维护者及获授权人员，不额外授予修改或编译权限。

## 当前版本

- 年级与学期选择，知识点的目标、讲解、分步例题、引导练习、独立练习、知识点闯关。
- 新增138个原创知识点、683个题型模板，覆盖十二个学期，含8,496道参数化练习和配套相似题；另保留原有6个知识点及36道题以兼容旧存档。每个新增知识点至少4类题型。每题包含答案、单位、分层提示、解题步骤、常见错误和建议时限。
- 每日6题跨知识点组合，优先练得较少的内容并加入已有错题；知识点练习和闯关按题型均衡选题，轮换未练过的参数题。星级、首次通关奖励和每日首次完成奖励。
- 精确识别整数、小数和等值分数，支持全角数字；题目要求最简分数时须约分，分类操作须选对全部图形；可以省略题目单位，填写单位时必须匹配。
- 错题自动保存，相似题独立答对后移除，使用提示仍有积分但不立即移除错题。
- 按年级、难度奖励积分，重复做对和使用提示奖励递减，答错不扣分。
- 三只独立宠物：主力选择、改名、照顾、20级与6级/13级进化、三槽换装；练习锁定奖励宠物。毛绒原画、眨眼/咀嚼和局部动作、动画/音效设置。
- 商店、背包、食物、永久玩具、蝴蝶结穿戴和家园装饰切换。
- 安卓本地 JSON 存档，原子替换；异常存档保留并备份，界面提示，不会自动覆盖。卸载/清除应用数据会失去本地进度。

**内容边界：** 原创题库按常用人教版知识范围及已选课程组织，不宣称已经逐册核定2026新版教材目录。题数包含同模板不同参数，不能当作独立题型数；所有标准答案经过精确验算，仍需教学审核与儿童体验校准。已加入12类图形练习（144道原题及144道新参数相似题，每个知识点至少4类题型），开放单元/学期综合关及独立成绩。综合学习内容仍需对照最新版教材持续审核。详见 [题型覆盖清单](docs/question-bank-coverage.md)。本批未导入GSM8K或其他第三方题库，题干、提示和解析独立编写。

修改与验证题库：

```bash
python3 scripts/generate-question-bank.py
python3 scripts/generate-question-bank.py --check
```

生成器同时更新离线JSON及覆盖清单，检查精确答案、相似题配对、题型数量和知识点内重复题干。题库作为程序集资源打进APK，不依赖网络加载。


## 安装到安卓设备

宠物美术已改为确认后的毛绒绘本风，九张透明角色与八种动作状态已经制作。用 `python3 scripts/package-pet-motion.py` 生成可离线打开的 `artifacts/mathpet-motion-preview.html`，或从 GitHub Actions 下载 `mathpet-pet-motion` 产物。详见 [动作资源说明](docs/pet-motion.md) 和 [已确认的美术方向](docs/pet-art-direction.md)。应用已接入三宠独立培养、20级进化、三槽换装、轻量表情与局部动作、三家园及音效设置。新增12类图形练习和单元/学期综合关。旧版 [SVG 资源页](src/MathPet.UI/wwwroot/pet-assets/gallery.html) 保留作历史版本。

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
dotnet restore tests/MathPet.Core.Tests --locked-mode
dotnet test tests/MathPet.Core.Tests --no-restore
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

## 从 GitHub 下载体验包

在仓库 **Actions → Android APK download** 中，打开成功的运行记录，在页面下方 **Artifacts** 下载 `mathpet-0.1.0-android`。下载的是 ZIP，解压后安装 APK。私有仓库需要登录你的 GitHub 账号才能下载，附件保留30天，过期后可点击 Run workflow 重新构建。

此自动构建使用 GitHub 临时机器上的开发签名，与云环境生成的签名不同，也不上传云端签名密钥。自动构建生成的体验包可能不能覆盖安装之前其他签名的版本；首次安装不受影响。正式更新渠道需要稳定、安全保存的签名。

本轮完成项及尚未验收内容见 [交付范围与结果](docs/completion-scope.md)。视频挂接见 [对应状态](docs/research/video-mapping-status.md)，官方教材证据见 [十二册目录核验状态](docs/research/curriculum-current-status.md)。新增专项页面检查：`MATHPET_TEST_URL=http://127.0.0.1:8773 node scripts/smoke-completion.cjs`（需先启动Web）。

题库后续审核与补齐结果见 [题库审核报告](docs/question-bank-audit.md)。独立验算运行 `python3 scripts/audit-question-bank.py`，其检查会在安卓CI构建前执行。

教学视频入口现已改为课堂内官方播放器，拦截B站App协议跳转。支持分集切换、重载和关闭；实际课程播放仍需设备验收。验证结果及云端HTTP412限制见 [应用内教学视频](docs/in-app-video.md)。

## 正式发行与应用更新

Android 已接入启动自动检查、设置页手动检查更新。进度自动保存在应用本地目录，不提供存档导入/导出入口。0.2.1 新增首页每日学习进度卡片。正式发行使用单独的 **Publish Android release** 工作流，固定签名、递增版本号，并上传公开的 APK、ZIP 与版本信息。首次发行前需要仓库所有者配置签名 Secrets；具体操作、旧体验包迁移及验证边界见 [更新与发行说明](docs/app-updates.md)。原有 Actions 体验包仍使用临时签名，不作为正式更新渠道。
