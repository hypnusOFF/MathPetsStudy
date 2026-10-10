const {chromium}=require('playwright'),assert=require('node:assert/strict'),fs=require('node:fs');
(async()=>{
 const topics=JSON.parse(fs.readFileSync('artifacts/visual-question-audit.json','utf8'));
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 try{
  const page=await browser.newPage({viewport:{width:412,height:915}}),errors=[];
  page.on('pageerror',e=>errors.push(e.message));
  await page.goto(process.env.MATHPET_TEST_URL||'http://127.0.0.1:8775');
  await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
  const nav=name=>page.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:new RegExp(name+'$')}).click();
  let checked=0;
  for(const topic of topics){
   await nav('学习课堂');await page.getByRole('button',{name:'一二三四五六'[topic.Grade-1]+'年级',exact:true}).click();
   await page.getByRole('combobox',{name:'学期'}).selectOption(topic.Semester===0?'First':'Second');
   await page.locator(`[data-topic-id=${topic.Id}]`).getByRole('button',{name:'去学习 →'}).click();
   await page.getByRole('button',{name:'5. 独立练习'}).click();await page.getByRole('button',{name:'开始独立练习 →'}).click();
   for(const q of [topic.Questions[0],...topic.Questions.filter(q=>q.Id.endsWith('-v0'))]){
    await page.getByTestId('question').filter({hasText:q.Text}).waitFor();
    const diagram=page.locator('.math-diagram');await diagram.waitFor();
    if(q.DiagramTask.startsWith('read-'))assert.equal(await diagram.locator('input[type=range]').count(),0,'read tasks must not overwrite numeric input');
    if(q.Interaction==='clock'&&q.DiagramTask===''){
     await diagram.locator('input[type=range]').nth(0).fill(String(q.Diagram[0]));await diagram.locator('input[type=range]').nth(1).fill(String(q.Diagram[1]));
     await page.waitForFunction(a=>document.querySelector('#answer').value===a,`${q.Diagram[0]}:${String(q.Diagram[1]).padStart(2,'0')}`);
    }else if(q.Interaction==='classify'&&!q.DiagramTask.startsWith('read-')){
     await page.locator('#answer').fill(q.Answer);await page.getByTestId('submit-answer').click();
     await page.getByTestId('answer-feedback').filter({hasText:'这次还差一点'}).waitFor();
     await page.getByRole('button',{name:/下一题|查看结算/}).click();
     // Continue the run after verifying count alone is rejected; selected-set acceptance is checked separately below.
     checked++;continue;
    }else if(q.Interaction==='reflect'&&q.DiagramTask===''){
     await page.getByRole('button',{name:`(${7-q.Diagram[0]},${q.Diagram[1]})`,exact:true}).click();
    }else if(q.Interaction==='rotate'&&q.DiagramTask===''){
     await diagram.locator('input[type=range]').fill(q.Answer);
    }else if(q.Interaction==='angle'&&q.DiagramTask===''){
     await diagram.locator('input[type=range]').fill(q.Answer);
    }else await page.locator('#answer').fill(q.Answer);
    await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();
    assert.doesNotMatch(await page.getByTestId('answer-feedback').innerText(),/@State\./);
    await page.getByRole('button',{name:/下一题|查看结算/}).click();checked++;
   }
   await page.getByRole('heading',{name:/练习完成/}).waitFor();
  }
  // Start a fresh classification session and submit both actual selection sets.
  await page.evaluate(()=>{const s=JSON.parse(localStorage.getItem('math-pet-csharp-v1'));s.Progress={};localStorage.setItem('math-pet-csharp-v1',JSON.stringify(s));});await page.reload();
  await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();await nav('学习课堂');
  await page.getByRole('button',{name:'一年级',exact:true}).click();await page.getByRole('combobox',{name:'学期'}).selectOption('Second');
  await page.locator('[data-topic-id=visual-classify]').getByRole('button',{name:'去学习 →'}).click();await page.getByRole('button',{name:'5. 独立练习'}).click();await page.getByRole('button',{name:'开始独立练习 →'}).click();
  for(const ids of [[1,3,5],[2,4]]){
   for(const i of ids)await page.getByRole('button',{name:i+'号图形',exact:true}).click();
   await page.waitForFunction(a=>document.querySelector('#answer').value===a,'选中：'+ids.join('、'));
   await page.getByTestId('submit-answer').click();await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();await page.getByRole('button',{name:/下一题/}).click();
  }
  assert.equal(checked,48);assert.deepEqual(errors,[]);
  console.log('PASS: 48 visual templates on mobile, numeric read tasks, clock/angle/rotation/reflection controls, wrong count rejection, triangle and rectangle exact selection.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
