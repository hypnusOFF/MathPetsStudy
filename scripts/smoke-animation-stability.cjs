const {chromium}=require('playwright');
const assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 try{
  const page=await browser.newPage({viewport:{width:412,height:915}});
  const errors=[];page.on('pageerror',e=>errors.push(e.message));
  await page.goto(process.env.MATHPET_TEST_URL||'http://127.0.0.1:8766');
  await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
  await page.getByRole('navigation').getByRole('button',{name:/宠物家园$/}).click();
  await page.waitForFunction(()=>[...document.querySelectorAll('.pet-motion img')].every(i=>i.complete&&i.naturalWidth>0));
  const image=await page.locator('.pet-base img').first().elementHandle();
  const motion=page.locator('.pet-motion').first();
  await page.getByRole('button',{name:'✨ 清洁 / 抚摸',exact:true}).click();
  await page.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='pet');
  assert(await image.evaluate(n=>n.isConnected),'Action start must retain the decoded image');
  assert.equal(await page.locator('.pet-base').first().evaluate(n=>getComputedStyle(n).maskImage),'none');
  assert.equal(await page.locator('.pet-expression').first().evaluate(n=>getComputedStyle(n).clipPath),'none');
  assert.equal(await page.locator('.pet-local').count(),0,'No moving fragments may expose holes');
  await motion.screenshot({path:'artifacts/screenshots/pet-action-fixed.png'});
  await page.getByRole('button',{name:'🧶 玩耍',exact:true}).click();
  await page.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='play');
  assert(await image.evaluate(n=>n.isConnected),'Cancelling another action must retain the image');
  await page.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='idle');
  assert(await image.evaluate(n=>n.isConnected),'Returning to idle must retain the image');
  for(const reduced of [false,true]){
   await page.evaluate(reduced=>{
    const s=JSON.parse(localStorage.getItem('math-pet-csharp-v1'));
    s.ReducedMotion=reduced;s.Food=5;s.Pets.forEach(p=>p.Fullness=0);
    localStorage.setItem('math-pet-csharp-v1',JSON.stringify(s));
   },reduced);
   await page.reload();await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
   await page.getByRole('navigation').getByRole('button',{name:/宠物家园$/}).click();
   const effects=()=>page.locator('.pet-motion').first().locator('.pet-effect').evaluateAll(nodes=>nodes.map(n=>n.className));
   await page.getByTestId('feed').click();
   await page.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='feed');
   assert.deepEqual(await effects(),['pet-effect pet-food'],`feed props, reduced=${reduced}`);
   await page.getByRole('button',{name:'🧶 玩耍',exact:true}).click();
   await page.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='play');
   assert.deepEqual(await effects(),['pet-effect pet-toy'],`play props, reduced=${reduced}`);
   await page.getByRole('button',{name:'✨ 清洁 / 抚摸',exact:true}).click();
   await page.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='pet');
   assert.deepEqual(await effects(),[],`petting props, reduced=${reduced}`);
   await page.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='idle');
   assert.deepEqual(await effects(),[],`idle props, reduced=${reduced}`);
  }
  assert.deepEqual(errors,[]);
  console.log('PASS: stable mobile pet images; feed only apple, play only ball, petting/idle no effects, in full and reduced motion.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
