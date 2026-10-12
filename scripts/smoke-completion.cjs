const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 try{
 const page=await browser.newPage({viewport:{width:1280,height:1000}}),errors=[],bad=[];
 page.on('pageerror',e=>errors.push(e.message));page.on('response',r=>{if(r.status()>=400)bad.push(r.url());});
 await page.goto(process.env.MATHPET_TEST_URL||'http://127.0.0.1:8770');await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
 const nav=name=>page.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:new RegExp(name+'$')}).click();
 await page.evaluate(()=>{const s=JSON.parse(localStorage.getItem('math-pet-csharp-v1'));s.Coins=10000;s.HasRoom=true;s.HasToy=true;s.Pets.forEach(p=>p.Experience=700);localStorage.setItem('math-pet-csharp-v1',JSON.stringify(s));});await page.reload();await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
 await nav('宠物家园');await page.locator('.pet-choice').first().waitFor();assert.equal(await page.locator('.pet-choice').count(),3);
 for(const name of ['糯糯','星星','团团']){const card=page.locator('.pet-choice').filter({has:page.getByRole('heading',{name,exact:false})});if(await card.getByRole('button',{name:'设为主力'}).count())await card.getByRole('button',{name:'设为主力'}).click();await page.getByRole('heading',{name:new RegExp(name+' ')}).first().waitFor();}
 await nav('设置');
 await page.getByLabel('播放音效').uncheck();await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')).SoundEnabled===false);
 await page.getByLabel('减少动画').check();await nav('宠物家园');await page.locator('.pet-motion[data-motion=reduced]').first().waitFor();
 await page.getByRole('button',{name:'小岛商店',exact:true}).click();await page.getByRole('heading',{name:'衣饰与穿搭',exact:true}).waitFor();assert.equal(await page.getByRole('button',{name:'购买衣饰',exact:true}).count(),9);for(let i=1;i<=9;i++){await page.getByRole('button',{name:'购买衣饰',exact:true}).first().click();await page.waitForFunction(n=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')).OwnedWearables.length===n,i);}await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')).OwnedWearables.length===9);
 await page.getByRole('button',{name:'查看背包'}).click();await page.getByRole('button',{name:'穿给团团',exact:true}).first().waitFor();assert.equal(await page.getByRole('button',{name:'穿给团团',exact:true}).count(),9);for(const b of await page.getByRole('button',{name:'穿给团团',exact:true}).all())await b.click();
 await page.waitForFunction(()=>{const s=JSON.parse(localStorage.getItem('math-pet-csharp-v1'));return s.Pets[0].Headwear==='star-clip'&&s.Pets[0].Outfit==='cape'&&s.Pets[0].Accessory==='badge';});await page.getByRole('button',{name:'星光小屋',exact:true}).click();await page.getByRole('button',{name:'← 宠物家园',exact:true}).click();
 await page.waitForFunction(()=>[...document.images].every(i=>i.complete&&i.naturalWidth>0));fs.mkdirSync('artifacts/screenshots',{recursive:true});await page.screenshot({path:'artifacts/screenshots/completion-pets.png',fullPage:true});
 const specs=[['classify',1,'Second'],['clock',2,'First'],['bars',3,'Second'],['angle',4,'First'],['reflect',4,'Second'],['rotate',5,'Second'],['pie',6,'First'],['ring',6,'Second'],['proportion',6,'Second'],['direction',4,'Second'],['views',2,'First'],['practice',5,'Second']];
 for(const [mode,grade,semester] of specs){
  await nav('学习课堂');await page.getByRole('button',{name:'一二三四五六'[grade-1]+'年级',exact:true}).click();await page.getByRole('combobox',{name:'学期'}).selectOption(semester);await page.locator(`[data-topic-id=visual-${mode}]`).getByRole('button',{name:'去学习 →'}).click();await page.getByRole('button',{name:'5. 独立练习'}).click();await page.getByRole('button',{name:'开始独立练习 →'}).click();await page.locator(`.math-diagram[data-interaction=${mode}]`).waitFor();
  if(mode==='classify'){for(const n of [1,3,5])await page.getByRole('button',{name:n+'号图形',exact:true}).click();await page.waitForFunction(()=>document.querySelector('#answer').value==='选中：1、3、5');await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();}
  if(mode==='clock'){const sliders=page.locator('.math-diagram input[type=range]');await sliders.nth(0).fill('3');await sliders.nth(1).fill('30');await page.waitForFunction(()=>document.querySelector('#answer').value==='3:30');await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();}
  if(mode==='reflect'){await page.getByRole('button',{name:'(6,2)',exact:true}).click();await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();}
  assert.doesNotMatch(await page.locator('body').innerText(),/@State\.|@pet\./);
  await page.screenshot({path:`artifacts/screenshots/visual-${mode}.png`,fullPage:true});await nav('学习首页');await page.getByRole('button',{name:'退出练习',exact:true}).click();await page.getByRole('dialog').waitFor({state:'hidden'});
 }
 assert.deepEqual(errors,[]);assert.deepEqual(bad,[]);console.log('PASS: three pets, nine wardrobe assets, mute/reduced motion, rooms, 12 visual exercise UIs, clock/classification/reflection submissions, no broken resources.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
