# Android 更新与发行

## 应用行为

Android 在启动进入首页后自动检查一次公开的 `Releases/latest/download/update.json`。
“应用更新与存档”可展开手动重试；离线、尚未发布、版本信息异常时，学习和养宠仍可继续。
以 Android `versionCode` 比较新旧，只有更高版本显示下载入口。只接受本仓库 HTTPS Release APK 地址。
下载按钮交由系统打开官方 APK 地址，下载后由用户确认安装，不会静默安装。
版本文件带 SHA-256，发行 ZIP 包含校验文件；当前系统浏览器下载流程不在应用内执行文件校验。
网页验证版不显示设备更新与备份入口。

首页可导出 JSON 存档，通过 Android 分享窗口保存到应用外。分享完成不等于保存成功，
请确认文件实际存在。导入先检查大小和完整存档规则，支持已有 v1–v4 迁移，确认后替换，
不会合并；替换前在应用私有目录保留原存档副本，写入失败不切换当前内存进度。
私有目录副本会随卸载删除，不能代替外部备份。不要公开上传个人存档。

## 固定签名：首次需仓库所有者操作

本会话可以推送源码，但 GitHub 对 Actions Secrets 管理返回 HTTP 403，不能代替所有者配置密钥。
签名私钥必须保存在仓库外并有独立安全备份，不能作为 Release 或 Actions artifact 上传。
正式流程不会在缺失密钥时生成随机密钥或降级到临时签名。

在自己的可信电脑上安装 JDK（提供 keytool）、GitHub CLI、OpenSSL 和 Bash，使用有本仓库 Secrets
管理权限的 GitHub 账号登录 `gh`，拉取最新代码后执行：

```bash
# 使用自己的私有备份目录；该目录必须在源码仓库外。
export MATHPET_SIGNING_DIR="$HOME/mathpet-private-signing"
bash scripts/configure-release-signing.sh
```

脚本创建固定 RSA 签名密钥（别名 mathpet、密钥与 keystore 共用密码），不打印密钥或密码；
通过标准输入上传两个 repository Actions secrets：

- `ANDROID_RELEASE_KEYSTORE_BASE64`：固定 keystore 的 Base64。
- `ANDROID_RELEASE_PASSWORD`：keystore 与签名密钥密码。

保留目录中的两个文件并安全备份。失败可重跑，已有密钥不会重建。
不要把秘密值发到聊天或提交到 Git。也可由所有者在仓库 Settings → Secrets and variables → Actions
手动配置上述两个 Secrets，内容须与别名 mathpet 的固定 keystore 匹配。

## 仅使用网页的签名配置方式

当 Codex 云代理仍将 GitHub API 请求识别为平台应用授权时，向云环境填写临时令牌
不一定能获得 Secrets 写权限。可以把相同临时令牌直接填入 GitHub 仓库 Actions Secrets，
由 GitHub 自身的 runner 配置签名，避免依赖云代理的凭据选择。

1. 打开仓库 Settings → Secrets and variables → Actions → New repository secret。
2. 名称填 `MATHPET_BOOTSTRAP_TOKEN`，值为仅授权本仓库 Secrets read/write 的临时令牌。
3. 在 main 上运行 Actions → **Set up Android signing**。

工作流先检查凭据权限，再生成固定密钥并把 keystore 和密码保存到两个发行 Secrets。
不会输出密码、上传私钥文件或替换已有完整签名。若仅有一个发行 Secret，停止处理，
避免误换已有签名。签名文件只在临时 runner 中存在，结束时清理。
此方式的持久密钥保存在 GitHub Actions Secrets；尚无独立离线备份，不能删除或覆盖它们。
GitHub Secrets API 无法读回秘密值。独立备份需要后续单独配置受保护的导出流程，
不能把原始密钥作为公开 Release 或 artifact 发布。

确认签名配置成功后，再运行正式发行工作流。临时令牌仅用于初始化，
成功后可删除 `MATHPET_BOOTSTRAP_TOKEN` 并在 GitHub 撤销令牌，正常发行不需要它。

## 发布

打开 Actions → **Publish Android release** → Run workflow，在 main 上输入未发布过的数字版本，
例如 `0.2.0`。流程从 main 构建，不从任意输入分支构建；校验题库、运行核心测试，
生成 Release 模式 APK，验证签名并发布 Release。一次操作后构建、打包和发布自动完成；
普通源码提交仅运行原有体验构建，不自动推送新版本给用户。

每次成功发行保留：APK、APK SHA-256、APK ZIP、`update.json` 和 `LICENSE`。
ZIP 内包含 APK、校验文件、版本信息和许可，适合分享下载。
`versionCode = 10000 + 此发行工作流的 run_number`，重跑同次运行不会改变代码，
重复版本禁止覆盖。不要删除重建此工作流或回退计数；未来更改编号方式须保证严格递增。
初始 csproj 的 0.1.0 / 1 为开发默认值，发行流程显式覆盖两个版本属性。

## 早期体验包迁移

已有 GitHub Actions 体验包每次使用临时开发签名，通常无法覆盖安装固定签名的新 APK。
新代码中的导出功能只有装上新代码后才能使用，不能追溯加入已安装的旧包。
因此，已有旧包且有重要进度时，先备份再卸载；不能直接卸载赌数据会恢复。

旧体验包为可调试构建，可以在自己的可信电脑上连接手机，开启 USB 调试并授权后，尝试：

```bash
adb exec-out run-as com.hypnus.mathpet cat files/player-v1.json > mathpet-backup.json
```

确认命令成功、文件非空且为包含 Version、Pets、Coins 等字段的 JSON，再保存独立副本。
这是旧包迁移备用方案，尚未在用户设备验证；若 run-as 失败，请保留旧应用，不要卸载。
若设备存在 Android 用户隔离或不是可调试构建，需要单独处理。
固定签名新版安装后，在学习首页选择导入备份并确认。
以后使用同一固定签名、更高 versionCode 更新通常无需卸载，可保留本地进度。

## 验证边界

本次本地构建使用独立的验证签名，绝不发布该测试密钥或测试签名安装包作为正式版。
Android 构建、签名检查和公开地址/版本文件验证不能替代真机验收；
需要设备检查分享保存、文件选择、导入、下载及连续两个固定签名版本的覆盖安装。
