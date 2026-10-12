const { chromium }=require('playwright');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const bank=JSON.parse(fs.readFileSync('src/MathPet.Core/Content/question-bank.json','utf8'));
const answers=new Map(bank.flatMap(t=>t.Questions.map(q=>[q.Text,q.Answer])));
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 const page=await browser.newPage({viewport:{width:1280,height:900}});page.setDefaultTimeout(15000);
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const url=process.env.MATHPET_TEST_URL||'http://127.0.0.1:8786';
 const stored=()=>page.evaluate(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')));
 const waitPetXp=async(species,xp)=>page.waitForFunction(({species,xp})=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.Pets.find(p=>p.Species===species)?.Experience===xp,{species,xp});
 const nav=async label=>page.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:new RegExp(label+'$')}).click();
 const pet=async()=>{await nav('宠物家园');await page.getByRole('heading',{name:'宠物家园',exact:true}).waitFor();};
 const shop=()=>page.getByRole('button',{name:'小岛商店',exact:true}).click();
 const bag=()=>page.getByRole('button',{name:'查看背包',exact:true}).click();
 const back=async()=>{await page.getByRole('button',{name:'← 宠物家园',exact:true}).click();await page.getByRole('heading',{name:'宠物家园',exact:true}).waitFor();};
 const selectPet=async species=>{if((await stored()).ActiveSpecies!==species){await page.locator(`[data-pet="${species}"]`).getByRole('button',{name:'设为主力',exact:true}).click();await page.waitForFunction(s=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.ActiveSpecies===s,species);}};
 await page.goto(url);await pet();await page.getByRole('button',{name:'测试：补充积分至 99999',exact:true}).click();
 await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.Version===4);
 await shop();
 for(const id of ['apple','cookie','rainbow-meal','star-feast'])await page.locator(`[data-food-item="${id}"]`).getByRole('button',{name:'购买食物',exact:true}).click();
 for(const id of ['plush-ball','puzzle-wheel','treasure-box'])await page.locator(`[data-toy-item="${id}"]`).getByRole('button',{name:'购买玩具',exact:true}).click();
 for(const id of ['dog','redpanda','otter'])await page.locator(`[data-pet-item="${id}"]`).getByRole('button',{name:'邀请伙伴',exact:true}).click();
 await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.Pets.length===6);
 assert.equal((await stored()).Coins,89799);await back();await selectPet('dog');
 await page.getByRole('combobox',{name:'选择食物'}).selectOption('cookie');await page.getByTestId('feed').click();await waitPetXp('dog',10);
 assert.equal((await stored()).FoodInventory.cookie,0);
 await page.getByRole('combobox',{name:'选择玩具'}).selectOption('treasure-box');await page.getByTestId('play').click();
 assert.equal((await stored()).Pets.find(p=>p.Species==='dog').ToyTrainingClaimed,false);
 await nav('每日练习');await page.getByRole('button',{name:/^开始今日练习/}).click();
 for(let i=0;i<6;i++){
  const text=await page.getByTestId('question').innerText();assert.ok(answers.has(text));
  await page.locator('#answer').fill(answers.get(text));await page.getByTestId('submit-answer').click();
  await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();await page.getByTestId('next-question').click();
  await page.getByTestId('answer-feedback').waitFor({state:'hidden'});
 }
 await page.getByRole('heading',{name:'练习完成！'}).waitFor();await waitPetXp('dog',46);
 await pet();await page.getByTestId('play').click();await waitPetXp('dog',64);
 assert.equal((await stored()).Pets.find(p=>p.Species==='dog').ToyTrainingClaimed,true);
 await page.reload();await pet();await page.getByRole('combobox',{name:'选择玩具'}).selectOption('puzzle-wheel');await page.getByTestId('play').click();
 await page.getByText('今天已经领取过训练经验，继续玩耍增加心情。',{exact:true}).waitFor();assert.equal((await stored()).Pets.find(p=>p.Species==='dog').Experience,64);
 await selectPet('redpanda');await page.getByRole('combobox',{name:'选择玩具'}).selectOption('treasure-box');await page.getByTestId('play').click();
 await page.getByRole('combobox',{name:'选择食物'}).selectOption('star-feast');await page.getByTestId('feed').click();await waitPetXp('redpanda',60);
 assert.equal((await stored()).Pets.find(p=>p.Species==='redpanda').ToyTrainingClaimed,false);
 fs.mkdirSync('artifacts/economy-v2-preview',{recursive:true});
 for(const [species,theme] of [['dog','forest'],['redpanda','autumn'],['otter','ocean']]){
  await selectPet(species);await shop();await page.locator(`[data-room-item="${theme}"]`).getByRole('button',{name:'解锁背景',exact:true}).click();
  const ids=await page.locator(`[data-decor-item^="${theme}-"]`).evaluateAll(items=>items.map(i=>i.dataset.decorItem));assert.equal(ids.length,5);
  for(const id of ids)await page.locator(`[data-decor-item="${id}"]`).getByRole('button',{name:'购买装饰',exact:true}).click();
  await bag();const clear=page.getByRole('button',{name:'收起全部装饰',exact:true});if(await clear.isEnabled())await clear.click();
  await page.locator(`[data-room-item="${theme}"]`).getByRole('button',{name:'使用背景',exact:true}).click();
  for(const id of ids)await page.locator(`[data-decor-item="${id}"]`).getByRole('button',{name:'摆放装饰',exact:true}).click();
  await back();await page.waitForFunction(()=>[...document.querySelectorAll('.room-canvas img')].every(i=>i.complete&&i.naturalWidth>0));
  await page.locator('.room-canvas').screenshot({path:`artifacts/economy-v2-preview/${species}-${theme}.png`,animations:'disabled'});
 }
 for(const width of [390,768,1280]){await page.setViewportSize({width,height:900});assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));}
 const assets=['care-assets/items.png',...['dog','redpanda','otter'].flatMap(id=>[`pet-animation/new-pets/${id}-open.png`,`pet-animation/new-pets/${id}-closed.png`])];
 for(const path of assets){const r=await page.request.get(url+'/_content/MathPet.UI/'+path);assert.equal(r.status(),200,path);}
 assert.equal(errors.length,0,errors.join('\n'));
 const snapshot=await stored();
 for(const species of ['dog','redpanda','otter'])for(const [stage,xp,position] of [['baby',0,'0% 0%'],['young',360,'50% 0%'],['mature',2110,'100% 0%']]){
  const context=await browser.newContext({viewport:{width:1280,height:900}});const preview=await context.newPage();
  const seeded=structuredClone(snapshot);seeded.ActiveSpecies=species;seeded.Pets.find(p=>p.Species===species).Experience=xp;
  await preview.addInitScript(s=>localStorage.setItem('math-pet-csharp-v1',JSON.stringify(s)),seeded);
  await preview.goto(url);await preview.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:/宠物家园$/}).click();
  const motion=preview.locator('.room-canvas .pet-motion');await motion.waitFor();assert.equal(await motion.getAttribute('data-stage'),stage);
  assert.equal(await motion.locator('.pet-base .pet-atlas').evaluate(e=>getComputedStyle(e).backgroundPosition),position);
  await preview.locator('.room-canvas').screenshot({path:`artifacts/economy-v2-preview/${species}-${stage}.png`,animations:'disabled'});
  await preview.addStyleTag({content:'.pet-expression{opacity:1!important;animation:none!important}'});
  assert.equal(await motion.locator('.pet-expression .pet-atlas').evaluate(e=>getComputedStyle(e).backgroundPosition),position);
  await preview.locator('.room-canvas').screenshot({path:`artifacts/economy-v2-preview/${species}-${stage}-closed.png`,animations:'disabled'});
  await context.close();
 }
 const legacy=await browser.newContext();const old=await legacy.newPage();
 const save={Version:3,Coins:234,Food:9,HasToy:true,Room:'stars',OwnedRooms:['study','spring','stars'],OwnedRoomDecorations:['stars-rug'],PlacedRoomDecorations:['stars-rug'],Pets:[{Species:'cat',Name:'团团',Experience:735,Fullness:65,Mood:80},{Species:'rabbit',Name:'糯糯',Experience:0,Fullness:65,Mood:80},{Species:'fox',Name:'星星',Experience:0,Fullness:65,Mood:80}]};
 await old.addInitScript(s=>localStorage.setItem('math-pet-csharp-v1',JSON.stringify(s)),save);await old.goto(url);
 await old.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:/宠物家园$/}).click();
 await old.locator('[data-pet="rabbit"]').getByRole('button',{name:'设为主力',exact:true}).click();
 await old.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1'))?.Version===4);
 const migrated=await old.evaluate(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')));assert.equal(migrated.Pets[0].Experience,2329);assert.equal(migrated.Coins,234);assert.equal(migrated.Food,9);assert.equal(migrated.HasToy,true);assert.equal(migrated.Room,'stars');
 await browser.close();console.log('PASS: typed food and toys, training eligibility/cap/reload, 3 paid pets with 3 stages and closed eyes, 3 new themes, responsive layouts, v3 migration');
})().catch(e=>{console.error(e);process.exit(1);});
