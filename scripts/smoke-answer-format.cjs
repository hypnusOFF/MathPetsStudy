const {chromium}=require('playwright'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 try{
  const page=await browser.newPage();
  await page.goto(process.env.MATHPET_TEST_URL||'http://127.0.0.1:8766');
  await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
  await page.getByRole('navigation').getByRole('button',{name:/学习课堂$/}).click();
  await page.getByRole('button',{name:'四年级',exact:true}).click();
  await page.getByRole('combobox',{name:'学期'}).selectOption('First');
  await page.locator('[data-topic-id=pep-g4-s1-058]').getByRole('button',{name:'去学习 →'}).click();
  await page.getByRole('button',{name:'5. 独立练习'}).click();
  await page.getByRole('button',{name:'开始独立练习 →'}).click();
  await page.getByTestId('question').filter({hasText:'20017'}).waitFor();
  await page.locator('#answer').fill('两万');await page.getByTestId('submit-answer').click();
  await page.getByRole('alert').filter({hasText:'无法识别'}).waitFor();
  assert.equal(await page.getByTestId('answer-feedback').count(),0);
  assert(await page.getByTestId('submit-answer').isEnabled());
  let state=await page.evaluate(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')));
  assert.equal(state.Mistakes.length,0);assert.equal(state.Progress['pep-g4-s1-058-q01'],undefined);
  await page.locator('#answer').fill('2万');await page.getByTestId('submit-answer').click();
  await page.getByTestId('answer-feedback').filter({hasText:'答对啦'}).waitFor();
  state=await page.evaluate(()=>JSON.parse(localStorage.getItem('math-pet-csharp-v1')));
  assert.equal(state.Mistakes.length,0);assert.equal(state.Progress['pep-g4-s1-058-q01'].AttemptCount,1);
  assert.equal(state.Progress['pep-g4-s1-058-q01'].CorrectCount,1);
  console.log('PASS: original place-value question accepts 2万; unsupported notation stays editable without mistakes or attempts.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
