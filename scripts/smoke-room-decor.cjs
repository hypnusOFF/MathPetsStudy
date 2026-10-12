/* Run against MathPet.Web: MATHPET_TEST_URL=http://127.0.0.1:8783 node scripts/smoke-room-decor.cjs */
const { chromium } = require('playwright');
const assert = require('node:assert/strict');
const fs = require('node:fs');
(async () => {
  const browser = await chromium.launch({executablePath:process.env.CHROMIUM_PATH || '/usr/bin/chromium',args:['--no-sandbox']});
  const page = await browser.newPage({viewport:{width:1280,height:900}});
  const errors=[]; page.on('pageerror',e=>errors.push(e.message));
  page.setDefaultTimeout(12000);
  const base=process.env.MATHPET_TEST_URL || 'http://127.0.0.1:8783';
  const stored=()=>page.evaluate(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')));
  const pet=async()=>{
    await page.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:/宠物家园$/}).click();
    await page.getByRole('heading',{name:'宠物家园',exact:true}).waitFor();
  };
  const shop=()=>page.getByRole('button',{name:'小岛商店',exact:true}).click();
  const bag=()=>page.getByRole('button',{name:'查看背包',exact:true}).click();
  await page.goto(base); await pet();
  await page.getByRole('button',{name:'测试：补充积分至 99999',exact:true}).click();
  await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.Coins===99999);
  assert.equal((await stored()).Coins,99999);
  await shop();
  assert.equal(await page.getByRole('heading',{name:'薄荷背带衣',exact:true}).count(),0);
  for(const id of ['spring','stars','forest','ocean','autumn']) await page.locator(`[data-room-item="${id}"]`).getByRole('button',{name:'解锁背景',exact:true}).click();
  const decorationIds=await page.locator('[data-decor-item]').evaluateAll(nodes=>nodes.map(n=>n.dataset.decorItem));
  assert.equal(decorationIds.length,30);
  for(const id of decorationIds) await page.locator(`[data-decor-item="${id}"]`).getByRole('button',{name:'购买装饰',exact:true}).click();
  await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.OwnedRoomDecorations.length===30);
  const bought=await stored(); assert.equal(bought.OwnedRoomDecorations.length,30);
  assert.equal(bought.PlacedRoomDecorations.length,0);
  await bag();
  fs.mkdirSync('artifacts/room-preview',{recursive:true});
  for(const theme of ['spring','stars','study','forest','ocean','autumn']) {
    for(const id of decorationIds.filter(id=>id.startsWith(theme+'-')))
      await page.locator(`[data-decor-item="${id}"]`).getByRole('button',{name:'摆放装饰',exact:true}).click();
    const roomButton=page.locator(`[data-room-item="${theme}"]`).getByRole('button',{name:'使用背景',exact:true});
    if(await roomButton.count()) await roomButton.click();
    await page.getByRole('button',{name:'← 宠物家园',exact:true}).click();
    await page.getByRole('heading',{name:'宠物家园',exact:true}).waitFor();
    const canvas=page.locator('.room-canvas');
    await page.waitForFunction(()=>[...document.querySelectorAll('.room-canvas img')].every(i=>i.complete&&i.naturalWidth>0));
    assert.equal(await page.locator('.room-decoration').count(),5);
    await canvas.screenshot({path:`artifacts/room-preview/${theme}.png`});
    await shop(); await bag();
    await page.getByRole('button',{name:'收起全部装饰',exact:true}).click();
  }
  for(const id of ['stars-mobile','spring-bench','study-rug'])
    await page.locator(`[data-decor-item="${id}"]`).getByRole('button',{name:'摆放装饰',exact:true}).click();
  await page.locator('[data-room-item="spring"]').getByRole('button',{name:'使用背景',exact:true}).click();
  await page.getByRole('button',{name:'← 宠物家园',exact:true}).click();
    await page.getByRole('heading',{name:'宠物家园',exact:true}).waitFor();
  const getPosition=()=>page.locator('[data-decoration="stars-mobile"]').getAttribute('style');
  const position=await getPosition();
  await page.locator('[data-pet="rabbit"]').getByRole('button',{name:'设为主力',exact:true}).click();
  await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.ActiveSpecies==='rabbit');
  assert.equal(await getPosition(),position);
  await shop();await bag();
  await page.locator('[data-room-item="stars"]').getByRole('button',{name:'使用背景',exact:true}).click();
  await page.getByRole('button',{name:'← 宠物家园',exact:true}).click();
    await page.getByRole('heading',{name:'宠物家园',exact:true}).waitFor();
  assert.equal(await getPosition(),position);
  await page.reload();await pet();
  assert.equal((await stored()).Room,'stars');
  assert.equal((await stored()).ActiveSpecies,'rabbit');
  assert.equal(await page.locator('.room-decoration').count(),3);
  await page.waitForFunction(()=>[...document.querySelectorAll('.room-canvas img')].every(i=>i.complete&&i.naturalWidth>0));
  await page.locator('.room-canvas').screenshot({path:'artifacts/room-preview/mixed.png'});
  for(const width of [390,768,1280]) {
    await page.setViewportSize({width,height:900});
    const geometry=await page.locator('.room-canvas').evaluate(canvas=>{
      const c=canvas.getBoundingClientRect(),d=canvas.querySelector('[data-decoration="stars-mobile"]').getBoundingClientRect();
      return {ratio:c.width/c.height,x:(d.x+d.width/2-c.x)/c.width,y:(d.y+d.height/2-c.y)/c.height};
    });
    assert.ok(Math.abs(geometry.ratio-1.6)<0.01);
    assert.ok(Math.abs(geometry.x-0.19)<0.005);assert.ok(Math.abs(geometry.y-0.22)<0.005);
    const overflow=await page.evaluate(()=>[...document.querySelectorAll('body *')].filter(e=>e.getBoundingClientRect().right>innerWidth+1).map(e=>({tag:e.tagName,classes:e.className,right:e.getBoundingClientRect().right})).slice(0,12));
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true,JSON.stringify({width,overflow}));
  }
  const coins=(await stored()).Coins;
  await page.getByRole('button',{name:'测试：补充积分至 99999',exact:true}).click();
  await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.Coins>=99999);
  assert.equal((await stored()).Coins,Math.max(coins,99999));
  assert.equal((await stored()).OwnedRoomDecorations.length,30);
  assert.equal(errors.length,0,errors.join('\n'));
  await browser.close(); console.log('PASS: 6 rooms, 30 decorations, cross-theme placement, persistence, mobile coordinates, test coins');
})().catch(e=>{console.error(e);process.exit(1);});
