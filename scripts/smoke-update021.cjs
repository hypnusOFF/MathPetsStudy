const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs');
const bank=JSON.parse(fs.readFileSync('src/MathPet.Core/Content/question-bank.json','utf8'));
const answers=new Map(bank.flatMap(t=>[...t.Questions,...t.ReviewQuestions]).map(q=>[q.Text,q.Answer]));
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 try{
 const page=await browser.newPage({viewport:{width:1280,height:900}});page.setDefaultTimeout(15000);
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const nav=name=>page.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:new RegExp(name+'$')}).click();
 const ready=()=>page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
 const stored=()=>page.evaluate(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')));
 await page.goto(process.env.MATHPET_TEST_URL||'http://127.0.0.1:8791');await ready();
 // An actual v4 save without the new optional fields is retained on load.
 await page.evaluate(()=>{const s=JSON.parse(localStorage.getItem('math-pet-csharp-v1'));delete s.LearningDate;delete s.DailyAnsweredCount;s.Coins=45678;s.Food=9;s.Pets[0].Name='更新验收';localStorage.setItem('math-pet-csharp-v1',JSON.stringify(s));});
 await page.reload();await ready();assert.equal(await page.getByTestId('daily-learning-count').innerText(),'0/6');
 let s=await stored();assert.equal(s.Coins,45678);assert.equal(s.Food,9);assert.equal(s.Pets[0].Name,'更新验收');
 await nav('每日练习');await page.getByRole('button',{name:'开始今日练习 →'}).click();
 await page.getByTestId('submit-answer').click();await page.getByText('请先输入答案。',{exact:true}).waitFor();assert.equal((await stored()).DailyAnsweredCount,0);
 await page.locator('#answer').fill('-999');await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').waitFor();
 await nav('学习首页');await page.getByRole('button',{name:'退出练习',exact:true}).click();await ready();assert.equal(await page.getByTestId('daily-learning-count').innerText(),'1/6');
 await page.reload();await ready();assert.equal(await page.getByTestId('daily-learning-count').innerText(),'1/6');
 await nav('每日练习');await page.getByRole('button',{name:'开始今日练习 →'}).click();
 for(let i=0;i<6;i++){
   const text=await page.getByTestId('question').innerText();assert.ok(answers.has(text),text);
   await page.locator('#answer').fill(answers.get(text));await page.getByTestId('submit-answer').click();
   await page.getByTestId('answer-feedback').waitFor();await page.getByTestId('next-question').click();await page.getByTestId('answer-feedback').waitFor({state:'hidden'});
 }
 await nav('学习首页');await ready();assert.equal(await page.getByTestId('daily-learning-count').innerText(),'6/6');await page.getByText('今日目标达成 ✓',{exact:true}).waitFor();
 await nav('设置');await page.getByRole('heading',{name:'应用更新',exact:true}).waitFor();
 for(const name of ['导出存档','导入存档','确认替换进度'])assert.equal(await page.getByRole('button',{name,exact:true}).count(),0);
 await page.getByLabel('播放音效').uncheck();await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')).SoundEnabled===false);
 await page.getByLabel('减少动画').check();await page.waitForFunction(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')).ReducedMotion===true);
 await page.reload();await ready();assert.equal(await page.getByTestId('daily-learning-count').innerText(),'6/6');
 s=await stored();assert.equal(s.Pets[0].Name,'更新验收');assert.equal(s.Food,9);assert.ok(s.Coins>=45678);assert.equal(s.SoundEnabled,false);assert.equal(s.ReducedMotion,true);
 for(const width of [390,768,1280]){await page.setViewportSize({width,height:900});await nav('设置');await page.getByRole('heading',{name:'应用更新',exact:true}).waitFor();assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`settings overflow ${width}`);await nav('学习首页');await ready();assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),`home overflow ${width}`);}
 fs.mkdirSync('artifacts/screenshots',{recursive:true});await page.screenshot({path:'artifacts/screenshots/update021-home.png',fullPage:true});await nav('设置');await page.screenshot({path:'artifacts/screenshots/update021-settings.png',fullPage:true});
 assert.deepEqual(errors,[]);console.log('PASS: 0.2.1 daily goal, partial progress, v4 compatibility, local persistence, settings and mobile layouts.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
