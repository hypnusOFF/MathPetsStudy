# Android 更新与发行

## 应用行为

Android 在启动进入首页后自动检查一次公开的 `Releases/latest/download/update.json`。
0.2.1 起在“设置 → 应用更新”手动重试；发现新版本时也会在其他页面显示更新提示。离线、尚未发布、版本信息异常时，学习和养宠仍可继续。
以 Android `versionCode` 比较新旧，只有更高版本显示下载入口。只接受本仓库 HTTPS Release APK 地址。
下载按钮交由系统打开官方 APK 地址，下载后由用户确认安装，不会静默安装。
版本文件带 SHA-256，发行 ZIP 包含校验文件；当前系统浏览器下载流程不在应用内执行文件校验。
网页验证版的设置页面只显示版本及说明，不执行 APK 更新。

0.2.1 起移除存档导入、导出入口，进度自动保存在 Android 应用私有目录的 `player-v1.json`。
同签名且版本号递增的覆盖更新沿用原存档；卸载或清除数据会删除进度。
首页新增每日学习进度，统计本地当天提交的有效答案，各种练习均计入，答错也计入，
格式错误要求重填的不计入。目标上限为 6 题，不额外发奖励；跨天重新开始。
新字段兼容原 v4 存档，已有当日每日练习完成记录可显示 6/6，其他旧答题历史不追溯统计。
0.2.0 的导入、导出功能仅保留为历史版本说明，当前版本不再提供。

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

## 更新验收

已安装的 0.2.0 可从原“应用更新与存档”入口检查并下载 0.2.1；无需卸载。
覆盖安装后可看到首页“每日学习进度”和导航中的“设置”，主动检查更新已移入设置。
验证宠物名字、积分、食物和学习记录保留。0.2.1 安装后显示当前已是最新版本属于正常结果。
早期临时签名体验包无法保证覆盖安装；开发测试若不保留进度，可以卸载后首次安装正式版。
未来不要为了覆盖更新而卸载已有正式版。

## 验证边界

本次本地构建使用独立的验证签名，绝不发布该测试密钥或测试签名安装包作为正式版。
Android 构建、签名检查和公开地址/版本文件验证不能替代真机验收；
需要设备检查更新发现、系统下载及连续两个固定签名版本的覆盖安装和存档保留。
