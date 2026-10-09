const { chromium } = require('playwright');
const assert = require('node:assert/strict');
(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', args: ['--no-sandbox'] });
  try {
    const page = await browser.newPage({ viewport: { width: 1280, height: 1000 } });
    const errors=[], requests=[];
    page.on('pageerror', e=>errors.push(e.message));
    page.on('request', r=>{if(r.url().startsWith('http'))requests.push(r.url());});
    await page.goto(process.env.PET_MOTION_URL || 'http://127.0.0.1:8769/mathpet-motion-preview.html');
    await page.waitForFunction(()=>[...document.images].every(i=>i.complete&&i.naturalWidth>0));
    assert.equal(await page.locator('#catalog .card').count(),9);
    // Verify real animation changes before installing the timer clock.
    const first=await page.locator('#actor .pet-sprite').evaluate(n=>getComputedStyle(n).transform);
    await page.waitForTimeout(450);
    assert.notEqual(await page.locator('#actor .pet-sprite').evaluate(n=>getComputedStyle(n).transform),first);
    await page.clock.install();
    let combinations=0;
    for(const kind of ['cat','rabbit','fox']){
      await page.locator(`#species button[data-value="${kind}"]`).click();
      for(const stage of ['baby','young','mature']){
        await page.locator(`#stages button[data-value="${stage}"]`).click();
        await page.waitForFunction(()=>document.querySelector('#actor .pet-sprite').naturalWidth>0);
        for(const action of ['pet','feed','play','correct','encourage','upgrade']){
          await page.locator(`#actions button[data-value="${action}"]`).click();
          assert.equal(await page.locator('#actor .pet-motion').getAttribute('data-action'),action);
          assert.notEqual(await page.locator('#actor .pet-sprite').evaluate(n=>getComputedStyle(n).animationName),'plush-breathe');
          await page.clock.fastForward(3000);
          assert.equal(await page.locator('#actor .pet-motion').getAttribute('data-action'),'idle');
          combinations++;
        }
      }
      await page.locator('#stages button[data-value="baby"]').click();
      for(const stage of ['young','mature']){
        await page.locator('#actions button[data-value="evolve"]').click();
        assert.equal(await page.locator('#actor .pet-old').count(),1);
        assert.match(await page.locator('#actor .pet-sprite:not(.pet-old)').getAttribute('class'),new RegExp(stage));
        await page.clock.fastForward(2800);
        assert.equal(await page.locator('#actor .pet-old').count(),0);
      }
      assert.equal(await page.locator('#actions button[data-value="evolve"]').isDisabled(),true);
    }
    await page.locator('#stages button[data-value="baby"]').click();
    await page.locator('#actions button[data-value="evolve"]').click();
    await page.locator('#species button[data-value="rabbit"]').click();
    await page.clock.fastForward(3000);
    assert.equal(await page.locator('#actor .pet-old').count(),0);
    await page.check('#reduced');
    await page.locator('#actions button[data-value="feed"]').click();
    assert.equal(await page.locator('#actor .pet-sprite').evaluate(n=>getComputedStyle(n).animationName),'none');
    assert.equal(await page.locator('#actor .pet-food').evaluate(n=>getComputedStyle(n).opacity),'1');
    await page.clock.fastForward(3000);
    await page.screenshot({path:'artifacts/pet-motion-desktop.png',fullPage:true});
    await page.locator('#catalog').screenshot({path:'artifacts/pet-motion-catalog.png'});
    await page.setViewportSize({width:390,height:844});
    assert.equal(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),true);
    await page.screenshot({path:'artifacts/pet-motion-mobile.png',fullPage:true});
    assert.equal(requests.length,1,'Offline preview must make no asset/CDN requests');
    // Also check the OS reduce-motion preference without the preview checkbox.
    await page.emulateMedia({reducedMotion:'reduce'});await page.uncheck('#reduced');
    assert.equal(await page.locator('#actor .pet-sprite').evaluate(n=>getComputedStyle(n).animationName),'none');
    assert.deepEqual(errors,[]);
    console.log(`PASS preview: 9 sprites, ${combinations} interaction combinations, 6 evolutions, cancellation, real breathing, reduced motion, mobile, offline assets.`);

    const app=await browser.newPage();const appErrors=[],failed=[];
    app.on('pageerror',e=>appErrors.push(e.message));app.on('response',r=>{if(r.status()>=400)failed.push(r.url());});
    const base=process.env.MATHPET_TEST_URL||'http://127.0.0.1:8768';
    await app.goto(base);await app.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
    await app.waitForFunction(()=>document.querySelector('.pet-sprite')?.naturalWidth>0);
    await app.getByRole('navigation').getByRole('button',{name:/宠物家园$/}).click();
    await app.getByRole('button',{name:'🧶 玩耍',exact:true}).click();
    await app.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='play');
    await app.getByRole('button',{name:'✨ 清洁 / 抚摸',exact:true}).click();
    await app.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='pet');
    await app.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='idle');
    // Force a valid near-threshold save, then feed across the existing evolution gate.
    await app.evaluate(()=>{const s=JSON.parse(localStorage.getItem('math-pet-csharp-v1'));s.Growth=49;s.CompletedPractices=1;s.Fullness=60;s.Food=3;localStorage.setItem('math-pet-csharp-v1',JSON.stringify(s));});
    await app.reload();await app.getByRole('heading',{name:'今天，也进步一点点'}).waitFor();
    await app.getByRole('navigation').getByRole('button',{name:/宠物家园$/}).click();
    await app.getByTestId('feed').click();
    await app.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='evolve');
    await app.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='idle');
    assert.match(await app.locator('.pet-sprite:not(.pet-effect)').getAttribute('src'),/cat-young/);
    await app.getByTestId('feed').click();
    await app.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='feed');
    await app.waitForFunction(()=>document.querySelector('.pet-motion').dataset.action==='idle');
    await app.getByTestId('feed').click(); // Already full; no false eating animation.
    await app.getByText('团团已经吃饱了，点心先存着吧。',{exact:true}).waitFor();
    assert.equal(await app.locator('.pet-motion').getAttribute('data-action'),'idle');
    await app.getByRole('button',{name:'🧶 玩耍',exact:true}).click();
    await app.getByRole('navigation').getByRole('button',{name:/学习首页$/}).click();
    await app.waitForTimeout(2500); // Cancelled component must not affect the new home portrait.
    assert.equal(await app.locator('.pet-motion').getAttribute('data-action'),'idle');
    assert.deepEqual(appErrors,[]);assert.deepEqual(failed,[]);
    console.log('PASS app: new sprites, pet/play cancellation, successful feed, evolution, full-pet refusal, navigation disposal; no console/network errors.');
  }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1});
