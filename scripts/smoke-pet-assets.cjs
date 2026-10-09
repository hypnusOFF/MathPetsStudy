// Run against the generated gallery served by a local static server.
const { chromium } = require('playwright');
const assert = require('node:assert/strict');

(async () => {
  const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || '/usr/bin/chromium', args: ['--no-sandbox'] });
  try {
    const page = await browser.newPage({ viewport: { width: 1360, height: 1024 } });
    const errors = [], external = [];
    page.on('pageerror', error => errors.push(error.message));
    const url = process.env.PET_ASSETS_URL || 'http://127.0.0.1:8767/gallery.html';
    page.on('request', request => { if (new URL(request.url()).origin !== new URL(url).origin) external.push(request.url()); });
    await page.goto(url);
    await page.clock.install();
    assert.equal(await page.locator('#catalog .card').count(), 9);
    assert.equal(await page.locator('#wardrobe .item').count(), 9);
    assert.equal(await page.locator('#items .item').count(), 14);
    assert.equal(await page.locator('#roomlist > div').count(), 3);
    // Check every garment at every species/stage against its moving slot.
    let combinations = 0;
    for (const species of ['cat', 'rabbit', 'fox']) {
      await page.locator(`#species button[data-value="${species}"]`).click();
      for (const stage of ['baby', 'young', 'mature']) {
        await page.locator(`#stages button[data-value="${stage}"]`).click();
        for (const slot of ['head', 'body', 'accessory']) {
          const options = await page.locator(`#${slot} option`).evaluateAll(nodes => nodes.map(n => n.value).filter(Boolean));
          for (const item of options) {
            await page.selectOption(`#${slot}`, item);
            const target = item === 'backpack' ? 'back' : slot;
            assert.equal(await page.locator(`.actor [data-layer="slot-${target}"] > g`).count(), 1);
            assert.match(await page.locator(`.actor [data-layer="slot-${target}"] > g`).getAttribute('transform'), /^translate\(.+\) scale\(.+\) translate\(-90 -90\)$/);
            combinations++;
          }
          await page.selectOption(`#${slot}`, '');
        }
      }
    }
    for (const action of ['pet', 'feed', 'play', 'correct', 'encourage', 'upgrade']) {
      await page.locator(`#actions button[data-value="${action}"]`).click();
      assert.equal(await page.locator('.actor svg').getAttribute('data-action'), action);
      await page.clock.fastForward(3000);
      assert.equal(await page.locator('.actor svg').getAttribute('data-action'), 'idle');
    }
    await page.locator('#stages button[data-value="baby"]').click();
    await page.selectOption('#head', 'hat');
    await page.selectOption('#body', 'overalls');
    await page.selectOption('#accessory', 'backpack');
    for (const next of ['young', 'mature']) {
      await page.locator('#actions button[data-value="evolve"]').click();
      await page.clock.fastForward(1500);
      assert.match(await page.locator('#caption').innerText(), new RegExp(next === 'young' ? '成长' : '成熟'));
      assert.equal(await page.locator('.actor [data-layer="slot-back"] > g').count(), 1);
      await page.clock.fastForward(1500);
      assert.equal(await page.locator('.actor svg').getAttribute('data-action'), 'idle');
    }
    // Changing pets cancels an in-flight evolution rather than mutating the new pet.
    await page.locator('#stages button[data-value="baby"]').click();
    await page.locator('#actions button[data-value="evolve"]').click();
    await page.locator('#species button[data-value="cat"]').click();
    await page.clock.fastForward(3500);
    assert.match(await page.locator('#caption').innerText(), /团团猫 · 幼年/);
    await page.check('#reduced');
    assert.equal(await page.locator('.actor .rig').evaluate(node => getComputedStyle(node).animationName), 'none');
    await page.screenshot({ path: '/workspace/.cloud-setup/pet-assets-desktop.png', fullPage: true });
    await page.locator('#catalog').screenshot({ path: '/workspace/.cloud-setup/pet-assets-nine-forms.png' });
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
    await page.screenshot({ path: '/workspace/.cloud-setup/pet-assets-mobile.png', fullPage: true });
    assert.deepEqual(errors, []);
    assert.deepEqual(external, []);
    console.log(`Pet resource preview passed: ${combinations} wardrobe combinations, 8 action states, evolution, reduced motion, mobile layout, no external requests.`);
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
