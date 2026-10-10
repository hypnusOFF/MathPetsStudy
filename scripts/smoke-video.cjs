const {chromium}=require('playwright'),assert=require('node:assert/strict');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',args:['--no-sandbox']});
 try{
  for(const width of [1280,412]){
   const page=await browser.newPage({viewport:{width,height:915}});let popups=0;page.on('popup',()=>popups++);
   // Deterministic UI checks; live B站 media is verified separately, not mocked here as a playback claim.
   await page.route('https://player.bilibili.com/player.html*',r=>r.fulfill({contentType:'text/html',body:'<!doctype html><title>Player fixture</title><button>播放</button>'}));
   await page.goto(process.env.MATHPET_TEST_URL||'http://127.0.0.1:8776');
   await page.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
   const nav=name=>page.getByRole('navigation',{name:'主导航'}).getByRole('button',{name:new RegExp(name+'$')}).click();
   const open=async()=>{await nav('学习课堂');await page.locator('[data-topic-id=pep-g1-s1-001]').getByRole('button',{name:'去学习 →'}).click();await page.getByText('相关免费教学视频',{exact:true}).click();};
   await open();assert.equal(await page.locator('.video-list a').count(),0);
   const choices=page.locator('.video-choice');await choices.first().click();const iframe=page.locator('.video-frame iframe');await iframe.waitFor();
   let url=new URL(await iframe.getAttribute('src'));assert.equal(url.hostname,'player.bilibili.com');assert.equal(url.searchParams.get('p'),'10');assert.equal(url.searchParams.get('autoplay'),'0');
   const sandbox=(await iframe.getAttribute('sandbox')).split(' ');assert(!sandbox.includes('allow-top-navigation'));assert(!sandbox.includes('allow-popups'));
   const old=await iframe.elementHandle();await choices.nth(1).click();await page.waitForFunction(()=>document.querySelector('.video-frame iframe').src.includes('&p=14&'));assert(await old.evaluate(e=>!e.isConnected));
   const beforeReload=await iframe.elementHandle();await page.getByRole('button',{name:'重新加载视频',exact:true}).click();await page.waitForFunction(e=>!e.isConnected,beforeReload);
   const box=await iframe.boundingBox();assert(box.width<=width&&box.height>100);
   await page.getByRole('button',{name:'关闭视频',exact:true}).click();await iframe.waitFor({state:'detached'});assert.equal(await iframe.count(),0);
   await choices.first().click();await iframe.waitFor();await page.getByText('相关免费教学视频',{exact:true}).click();await iframe.waitFor({state:'detached'});assert.equal(await iframe.count(),0,'collapsing videos must stop playback');
   await page.getByText('相关免费教学视频',{exact:true}).click();await choices.first().click();await iframe.waitFor();await nav('宠物家园');await iframe.waitFor({state:'detached'});assert.equal(await iframe.count(),0,'leaving lesson must stop playback');
   assert.equal(popups,0);await page.close();
  }
  console.log('PASS: desktop/mobile inline player, correct parts, no external links/popups, reload, close, collapse and navigation unload. Fixture checks validate UI, not B站 media availability.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
