/* Start MathPet.Web before running: npm ci && npm run test:ui */
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({ ...(process.env.CHROMIUM_PATH ? {executablePath:process.env.CHROMIUM_PATH}:{}),args:['--no-sandbox'] });
  const page = await browser.newPage({viewport:{width:1360,height:960}});
  page.setDefaultTimeout(12000);
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  const base=process.env.MATHPET_TEST_URL || 'http://127.0.0.1:8766';
  const nav=label=>page.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:new RegExp(label+'$')}).click();
  const stored=()=>page.evaluate(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')));
  const answer=async(text)=>{await page.locator('#answer').fill(text);await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').waitFor();await page.getByTestId('next-question').click();};
  await page.goto(base);await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
  await page.getByRole('button',{name:'重置本机进度',exact:true}).click();await page.getByRole('button',{name:'确认重置',exact:true}).click();await page.getByRole('dialog').waitFor({state:'hidden'});
  await nav('学习课堂');await page.getByRole('button',{name:'去学习 →'}).click();await page.getByRole('button',{name:'3. 例题演示'}).click();await page.locator('details').nth(1).locator('summary').click();assert.match(await page.locator('main').innerText(),/选择加法/);
  await nav('每日练习');await page.getByRole('button',{name:'开始今日练习 →'}).click();
  // Empty answer stays on the question without granting points.
  await page.getByTestId('submit-answer').click();await page.getByText('请先输入答案。',{exact:true}).waitFor();
  await answer('5');await answer('0');await answer('7');await page.getByRole('heading',{name:'练习完成！'}).waitFor();
  let data=await stored();assert.equal(data.Mistakes.length,1);assert.equal(data.CompletedPractices,1);assert.equal(data.Coins,152);
  await nav('错题复习');await page.getByRole('button',{name:'试试相似题'}).click();assert.match(await page.getByTestId('question').innerText(),/9 − 2/);await answer('7');await page.getByRole('heading',{name:'练习完成！'}).waitFor();assert.equal((await stored()).Mistakes.length,0);
  await nav('闯关地图');await page.getByRole('button',{name:'六年级',exact:true}).click();await page.getByRole('button',{name:'开始闯关',exact:true}).click();await page.getByTestId('timer').waitFor();
  await answer('30');await answer('10人');await answer('64元');await page.getByRole('heading',{name:'挑战成功！'}).waitFor();assert.equal((await stored()).Stars.percent,3);
  await nav('宠物家园');await page.getByRole('button',{name:'小岛商店',exact:true}).click();
  const item=name=>page.locator('.shop-item').filter({has:page.getByRole('heading',{name,exact:true})});
  await item('紫色蝴蝶结').getByRole('button',{name:'购买',exact:true}).click();await page.getByRole('button',{name:'查看背包'}).click();await item('紫色蝴蝶结').getByRole('button',{name:'戴上',exact:true}).click();
  await page.getByRole('button',{name:'← 宠物家园'}).click();const food=(await stored()).Food;await page.getByTestId('feed').click();await page.waitForFunction(n=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')).Food===n-1,food);
  await page.getByRole('button',{name:'改名字',exact:true}).click();await page.locator('#pet-name').fill('小团');await page.getByRole('button',{name:'保存名字',exact:true}).click();await page.getByRole('dialog').waitFor({state:'hidden'});
  await page.reload();await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();data=await stored();assert.equal(data.PetName,'小团');assert.equal(data.WearingRibbon,true);assert.equal(data.Stars.percent,3);
  await nav('每日练习');await page.getByRole('button',{name:'五年级',exact:true}).click();await page.getByRole('button',{name:'开始今日练习 →'}).click();await page.locator('#answer').fill('10/12');await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();
  await nav('宠物家园');await page.getByRole('dialog').waitFor();await page.getByRole('button',{name:'退出练习',exact:true}).click();await page.getByRole('heading',{name:'宠物家园',exact:true}).waitFor();
  // Hints give learning rewards, but cannot pass an independent challenge.
  await nav('闯关地图');await page.getByRole('button',{name:'一年级',exact:true}).click();await page.getByRole('button',{name:'开始闯关',exact:true}).click();
  for(const text of ['5','5','7']) { await page.getByRole('button',{name:'💡 给我一点提示',exact:true}).click();await answer(text); }
  await page.getByRole('heading',{name:'再巩固一下就更好了'}).waitFor();assert.equal((await stored()).Stars.addition,undefined);
  fs.mkdirSync('artifacts/screenshots',{recursive:true});await nav('学习首页');await page.screenshot({path:'artifacts/screenshots/desktop.png',fullPage:true});
  await page.setViewportSize({width:390,height:844});
  for(const label of ['学习首页','学习课堂','每日练习','闯关地图','错题复习','宠物家园']){await nav(label);assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false,`${label} overflows`);}
  await page.screenshot({path:'artifacts/screenshots/mobile-pet.png',fullPage:true});await page.getByRole('button',{name:'小岛商店',exact:true}).click();assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth>innerWidth),false);
  // Malformed saves are preserved and explicitly reported instead of silently erased.
  const badPage=await browser.newPage();await badPage.goto(base);await badPage.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();await badPage.evaluate(()=>localStorage.setItem('math-pet-csharp-v1','broken-save'));await badPage.reload();await badPage.getByRole('alert').filter({hasText:'无法读取本地存档'}).waitFor();assert.equal(await badPage.evaluate(()=>localStorage.getItem('math-pet-csharp-v1')),'broken-save');await badPage.close();
  assert.deepEqual(errors,[]);console.log('PASS: C# UI lesson, scoring, daily, challenge, hints, review variants, pet shopping/care/dress/name, persistence, exit confirmation, malformed-save preservation and mobile layouts.');await browser.close();
})().catch(e=>{console.error(e);process.exit(1)});
