#!/usr/bin/env python3
"""Build the original offline bank; no website questions or assets are copied.

Each entry below names a concept, not an official textbook edition. Main/review
pairs share a template but use different parameters. Calculation is audit data,
not content shown to children. Run with --check to check reproducibility.
"""
import argparse
import ast
import json
from fractions import Fraction as F
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def fmt(x):
    x = F(x)
    if x.denominator == 1:
        return str(x.numerator)
    d = x.denominator
    for p in (2, 5):
        while d % p == 0:
            d //= p
    if d == 1:
        return f"{float(x):.6f}".rstrip('0').rstrip('.')
    return f"{x.numerator}/{x.denominator}"


def fraction(x):
    x = F(x)
    return str(x.numerator) if x.denominator == 1 else f"{x.numerator}/{x.denominator}"


def q(text, value, calculation, method, work, mistake, kind, unit=None, seconds=60, options=None):
    return dict(Text=text, Answer=fmt(value), Unit=unit,
                Hints=[method, work], Solution=[method, work, f"所以答案为 {fmt(value)}{unit or ''}。"],
                CommonMistake=mistake, TimeLimitSeconds=seconds,
                Difficulty=1.25 if kind in ('生活应用', '反向求量', '多步应用', '推理挑战') else 1,
                QuestionType=kind, Options=options or [], Calculation=calculation, Source="独立原创")


def judge(statement, correct, reason, mistake):
    return q(statement + '（正确选1，错误选0）', int(correct), str(int(correct)),
             '先检查数量关系或定义，再判断。', reason, mistake, '判断辨析', options=['错误', '正确'])


def arithmetic(c, k, n):
    limit = c['limit']; op = c['op']; a = 2 + n % max(2, limit // 3); b = 1 + (n * 3 + 2) % max(2, limit // 4)
    if 'digits' in c:
        low,high,lowb,highb=c['digits'];a=low+(n*17)%(high-low+1);b=lowb+(n*7)%(highb-lowb+1)
    if c.get('carry'):a=6+n%4;b=min(9,10-a+1+n%a)
    if c.get('borrow'):a=11+n%8;b=min(9,a-10+1+n%3)
    if c.get('decimal'):
        a, b = F(a, 10), F(b, 10)
    if op == '+':
        result = a + b; method = '把两部分合起来，用加法；小数加法要小数点对齐。' if c.get('decimal') else '把两部分合起来，用加法。'
    elif op == '-':
        if not ('digits' in c or c.get('borrow')):a += b
        if a<b:a,b=b,a
        result = a - b; method = '从总量中去掉一部分，用减法；小数点对齐。' if c.get('decimal') else '从总量中去掉一部分，用减法。'
    elif op == '*':
        if c.get('table'): a, b = 2 + n % 8, 2 + (n * 3) % 8
        result = a * b; method = '每份数量×份数=总量。'
    else:
        if c.get('decimal'): b = F(2 + n % 7, 10)
        else: b = (10+n%20) if c.get('two_digit_divisor') else 2 + n % (8 if c.get('table') else 17)
        a *= b; result = F(a) / F(b); method = '总量÷份数=每份数量。'
    sign = {'+': '+', '-': '−', '*': '×', '/': '÷'}[op]
    work = f'{fmt(a)} {sign} {fmt(b)} = {fmt(result)}。'
    expr = f'({a}){op}({b})'; mistake = '先看清运算符号；计算后用逆运算检查，不能把乘除当成加减。'
    if k == 0: return q(f'{fmt(a)} {sign} {fmt(b)} = ?', result, expr, method, work, mistake, '直接计算', seconds=45)
    if k == 1:
        inverse = {'+': f'{fmt(result)} − {fmt(b)}', '-': f'{fmt(result)} + {fmt(b)}', '*': f'{fmt(result)} ÷ {fmt(b)}', '/': f'{fmt(result)} × {fmt(b)}'}[op]
        return q(f'□ {sign} {fmt(b)} = {fmt(result)}，□里填多少？', a, f'({a})', '用相反的运算找回缺少的数。', f'{inverse} = {fmt(a)}。', '缺少的数不一定是结果，先看它在算式中的位置。', '逆运算填空')
    if k == 2:
        texts = {'+': f'小林有{fmt(a)}元，又收到{fmt(b)}元，现在有多少元？', '-': f'小林有{fmt(a)}元，买文具用掉{fmt(b)}元，还剩多少元？', '*': f'每米彩带{fmt(a)}元，买{fmt(b)}米需要多少元？', '/': f'{fmt(a)}米彩带平均分成{fmt(b)}份，每份多少米？'}
        if c.get('decimal') and op == '/': texts[op] = f'{fmt(a)}米彩带，每段{fmt(b)}米，可以剪成多少段？'
        unit = '段' if c.get('decimal') and op == '/' else '米' if op == '/' else '元'
        return q(texts[op], result, expr, method, work, mistake, '生活应用', unit)
    if k == 3:
        claim=result+int(n%3==0)
        return judge(f'{fmt(a)} {sign} {fmt(b)} = {fmt(claim)}', claim==result, work, '不能只看数字相近就判断正确。')
    target = result + 2
    return q(f'{fmt(a)} {sign} {fmt(b)} 的结果比{fmt(target)}少多少？', 2, f'({target})-({expr})', '先求算式结果，再求两个数相差多少。', work + f'再算{fmt(target)} − {fmt(result)} = 2。', '比较结果时还需要第二步，不能直接提交原算式的结果。', '多步应用')


def numbers(c, k, n):
    maximum = c['limit']; a = 1 + n % max(2, maximum - 1)
    if c.get('place'):
        place = c['place']; digit = 1 + n % max(1,min(9,maximum//place-1)); other = (n * 17) % place
        a = digit * place + other
        if k == 0: return q(f'{a}中的数字{digit}在最高位，它表示多少？', digit * place, f'{digit}*{place}', '最高位的计数单位决定数字表示的大小。', f'{digit}个{place}是{digit * place}。', '同一个数字在不同数位上表示的大小不同。', '数位意义')
        if k == 1: return q(f'{digit}个{place}和{other}个一合起来是多少？', a, f'{digit}*{place}+{other}', '按计数单位组成数。', f'{digit * place} + {other} = {a}。', '组成数时要按数位计算，不是把数字并排写。', '组成数')
        if k == 2: return q(f'{a} − {other} = ?', digit * place, f'{a}-{other}', '去掉个位单位的部分，留下整计数单位的部分。', f'{a} − {other} = {digit * place}。', '注意连续的0也占数位。', '分解数')
        if k == 3: return judge(f'{a}里有{digit}个完整的{place}', True, f'{a} = {digit} × {place} + {other}，余下部分小于{place}。', '问完整的计数单位个数时要看商，不是看总数。')
        return q(f'有{digit}袋珠子，每袋{place}颗，另外还有{other}颗，一共有多少颗？', a, f'{digit}*{place}+{other}', '先求整袋数量，再加零散数量。', f'{digit} × {place} + {other} = {a}。', '零散的珠子也要计入总数。', '生活应用', '颗')
    if k == 0: return q(f'按顺序填数：{a-1}、□、{a+1}。', a, str(a), '相邻两个整数相差1。', f'{a-1}后面是{a}，再后面是{a+1}。', '按从小到大的顺序填，不要倒着数。', '数的顺序')
    if k == 1: return q(f'{a}可以分成1和几？', a-1, f'{a}-1', '整体减去已知的一部分就是另一部分。', f'{a} − 1 = {a-1}。', '分成两部分后，合起来应等于原数。', '数的分解')
    if k == 2: return q(f'{a}和{a-1}，较大的数是多少？', a, str(a), '数轴上越靠右的数越大。', f'{a}比{a-1}大1。', '比较大小不要只看数字的形状。', '大小比较')
    if k == 3: return judge(f'{a}比{a-1}'+('少1' if n%3==0 else '多1'), n%3!=0, f'{a} − {a-1} = 1，所以是多1。', '分清“多”和“少”的方向。')
    return q(f'从左往右数，小林排第{a}，前面有几人？', a-1, f'{a}-1', '前面的人数不包括小林自己。', f'{a} − 1 = {a-1}。', '排第几表示顺序，不等于前面的人数。', '序数应用', '人')


def units(c, k, n):
    large, small, factor = c['large'], c['small'], c['factor']; a = 2 + n % 8; b = 1 + n % min(factor - 1, 9)
    method = f'1{large} = {factor}{small}。大单位化成小单位乘进率，小单位化成大单位除以进率。'
    mistake = '长度、面积、体积的进率不同；换算后再计算，结果单位必须一致。'
    if k == 0: return q(f'{a}{large} = 多少{small}？', a*factor, f'{a}*{factor}', method, f'{a} × {factor} = {a*factor}。', mistake, '单位换算', small)
    if k == 1: return q(f'{a*factor}{small} = 多少{large}？', a, f'{a*factor}/{factor}', method, f'{a*factor} ÷ {factor} = {a}。', mistake, '反向换算', large)
    if k == 2: return q(f'{a}{large}{b}{small}一共是多少{small}？', a*factor+b, f'{a}*{factor}+{b}', method, f'先算{a} × {factor}，再加{b}，得{a*factor+b}。', mistake, '复名数换算', small)
    if k == 3: return judge(f'{a}{large}与{a*factor}{small}一样多', True, f'{a} × {factor} = {a*factor}。', mistake)
    return q(f'一件材料的计量是{a}{large}，用掉{b}{small}，还剩多少{small}？', a*factor-b, f'{a}*{factor}-{b}', method, f'{a*factor} − {b} = {a*factor-b}。', mistake, '生活应用', small)


def fractions(c, k, n):
    mode = c['mode']; d = 3 + n % 7; a = 1 + n % (d-1); x = F(a,d); y = F(1,2*d)
    common = '分子表示取了几份，分母表示整体平均分成几份。'
    mistake = '分数必须对应同一个整体；约分时分子和分母同时除以同一个非零数。'
    if mode == 'meaning':
        if k == 0: return q(f'一张纸平均分成{d}份，涂了{a}份，涂色部分占整张纸的几分之几？',x,f'{a}/{d}',common,f'取{a}份，占{a}/{d}。',mistake,'分数表示')
        if k == 1: return q(f'一个整体平均分成{d}份，每份是整体的几分之几？',F(1,d),f'1/{d}',common,f'一份用1/{d}表示。',mistake,'单位分数')
        if k == 2: return q(f'一条彩带平均分成{d}份，用去{a}份，剩下占全长的几分之几？',1-x,f'1-{a}/{d}',common,f'还剩{d-a}份，占{d-a}/{d}。',mistake,'生活应用')
        if k == 3: return judge(f'把一个整体随意分成{d}份，每份一定是1/{d}',False,'只有平均分，每份才是整体的1/'+str(d)+'。',mistake)
        return q(f'计算{a}/{d} + 1/{d}。',x+F(1,d),f'{a}/{d}+1/{d}','同分母分数相加，分母不变，分子相加。',f'({a}+1)/{d} = {fraction(x+F(1,d))}。','不能把分母也相加。','同分母计算')
    if mode in ('equivalent','simplify','compare'):
        if k == 0:return q(f'把{a*2}/{d*2}化为最简分数。',x,f'{a*2}/{d*2}','找分子、分母的公因数，约分到互质。',f'{a*2}/{d*2} = {fraction(x)}。',mistake,'约分')
        if k == 1:return q(f'{a}/{d} = □/{d*3}，□里填多少？',a*3,f'{a}*3','分母乘3，分子也要乘3。',f'{a} × 3 = {a*3}。',mistake,'等值分数填空')
        if k == 2:return q(f'{a}/{d}与{a}/{d+1}相比，较大的分数是多少？',x,f'{a}/{d}','分子相同，分母越小，每份越大。',f'{d} < {d+1}，所以{a}/{d}较大。','不能认为分母大的分数一定大。','大小比较')
        if k == 3:return judge(f'{a}/{d} = {a*2}/{d*2}',True,'分子和分母同时乘2，分数大小不变。',mistake)
        return q(f'{d*6}颗珠子，红珠占{a}/{d}，红珠有多少颗？',a*6,f'{d*6}*{a}/{d}','先求一份有几颗，再乘份数。',f'{d*6} ÷ {d} × {a} = {a*6}。',mistake,'生活应用','颗')
    if mode == 'same': y=F(1,d); op='+'
    else: op={'add':'+','sub':'-','mul':'*','div':'/'}[mode]
    result={'+':x+y,'-':x-y,'*':x*y,'/':x/y}[op]; sign={'+':'+','-':'−','*':'×','/':'÷'}[op]
    method={'+' : '先通分，再把分子相加，最后约分。','-':'先通分，再把分子相减，最后约分。','*':'分子相乘、分母相乘；可以先约分。','/':'除以一个非零分数，等于乘这个分数的倒数。'}[op]
    work=f'{fraction(x)} {sign} {fraction(y)} = {fraction(result)}。'; expr=f'({x}){op}({y})'
    if k==0:return q(work.split(' = ')[0]+' = ?（输入分数）',result,expr,method,work,mistake,'直接计算',seconds=75)
    if k==1:return q(f'□ {sign} {fraction(y)} = {fraction(result)}，□是多少？',x,str(x),'用逆运算求缺少的分数。',f'代入{fraction(x)}，有{work}',mistake,'逆运算填空',seconds=90)
    if k==2:
        text={'+' : f'两段彩带分别长{fraction(x)}米和{fraction(y)}米，合起来多少米？','-':f'一段彩带长{fraction(x)}米，剪去{fraction(y)}米，还剩多少米？','*':f'一段彩带长{fraction(x)}米，取它的{fraction(y)}，取出多少米？','/':f'有{fraction(x)}米彩带，每段长{fraction(y)}米，可以剪成几段？'}[op]
        return q(text,result,expr,method,work,mistake,'生活应用','段' if op=='/' else '米',90)
    if k==3:return judge(f'{fraction(x)} {sign} {fraction(y)} = {fraction(result+int(n%3==0))}',n%3!=0,work,mistake)
    return q(f'先计算{fraction(x)} {sign} {fraction(y)}，再乘{d}，结果是多少？',result*d,f'({expr})*{d}',method,work+f'再乘{d}，得{fraction(result*d)}。','先算括号内，再计算下一步。','多步应用',seconds=100)


def geometry(c,k,n):
    mode=c['mode']; a=3+n%9; b=2+n%5; h=2+n%7; pi=F(314,100)
    # Expressions and dimensions use cm consistently; pi is explicitly fixed.
    rules={
        'perimeter':(2*(a+b),f'2*({a}+{b})',f'长{a}厘米、宽{b}厘米的长方形周长', '（长＋宽）×2', '厘米'),
        'square-perimeter':(4*a,f'4*{a}',f'边长{a}厘米的正方形周长','边长×4','厘米'),
        'rectangle':(a*b,f'{a}*{b}',f'长{a}厘米、宽{b}厘米的长方形面积','长×宽','平方厘米'),
        'square':(a*a,f'{a}*{a}',f'边长{a}厘米的正方形面积','边长×边长','平方厘米'),
        'parallelogram':(a*h,f'{a}*{h}',f'底{a}厘米、对应高{h}厘米的平行四边形面积','底×对应高','平方厘米'),
        'triangle':(F(a*h,2),f'{a}*{h}/2',f'底{a}厘米、对应高{h}厘米的三角形面积','底×对应高÷2','平方厘米'),
        'trapezoid':(F((a+b)*h,2),f'({a}+{b})*{h}/2',f'上底{a}厘米、下底{b}厘米、高{h}厘米的梯形面积','（上底＋下底）×高÷2','平方厘米'),
        'circle-perimeter':(2*pi*a,f'2*(314/100)*{a}',f'半径{a}厘米的圆的周长（π取3.14）','2×π×半径','厘米'),
        'circle':(pi*a*a,f'(314/100)*{a}*{a}',f'半径{a}厘米的圆的面积（π取3.14）','π×半径×半径','平方厘米'),
        'cuboid':(a*b*h,f'{a}*{b}*{h}',f'长{a}厘米、宽{b}厘米、高{h}厘米的长方体体积','长×宽×高','立方厘米'),
        'cube':(a**3,f'{a}**3',f'棱长{a}厘米的正方体体积','棱长×棱长×棱长','立方厘米'),
        'surface':(2*(a*b+a*h+b*h),f'2*({a}*{b}+{a}*{h}+{b}*{h})',f'长{a}厘米、宽{b}厘米、高{h}厘米的长方体表面积','（长×宽＋长×高＋宽×高）×2','平方厘米'),
        'cube-surface':(6*a*a,f'6*{a}**2',f'棱长{a}厘米的正方体表面积','棱长×棱长×6','平方厘米'),
        'cylinder':(pi*a*a*h,f'(314/100)*{a}**2*{h}',f'底面半径{a}厘米、高{h}厘米的圆柱体积（π取3.14）','底面积×高','立方厘米'),
        'cone':(pi*a*a*h/3,f'(314/100)*{a}**2*{h}/3',f'底面半径{a}厘米、高{h}厘米的圆锥体积（π取3.14，输入精确分数也可）','底面积×高÷3','立方厘米'),
        'cylinder-surface':(2*pi*a*a+2*pi*a*h,f'2*(314/100)*{a}**2+2*(314/100)*{a}*{h}',f'底面半径{a}厘米、高{h}厘米的封闭圆柱表面积（π取3.14）','两个底面积＋侧面积','平方厘米'),
        'composite':(a*b+b*h,f'{a}*{b}+{b}*{h}',f'两个没有重叠的长方形，长宽分别为{a}和{b}厘米、{b}和{h}厘米，合并图形的面积','分别求面积再相加','平方厘米'),
    }
    value,expr,stem,formula,unit=rules[mode];method=f'使用公式：{formula}。';work=f'代入已知长度，{expr.replace("*","×")} = {fmt(value)}。'
    mistake='看清求周长、面积还是体积；高必须与底对应，注意平方和立方单位。'
    if k==0:return q(f'{stem}是多少{unit}？',value,expr,method,work,mistake,'公式计算',unit,90)
    if k==1:
        return q(f'{stem}是{fmt(value)}{unit}。做两个完全相同的模型，合计的'+('体积' if '立方' in unit else '面积' if '平方' in unit else '周长')+f'是多少{unit}？',value*2,f'({expr})*2',method,work+f'两份相加：{fmt(value)} × 2 = {fmt(value*2)}。','这里求两个图形各自测量结果之和，不是拼接后的周长。','多步应用',unit,100)
    if k==2:
        # Genuine inverse length/height questions for shapes with linear height.
        linear={'rectangle':(a,b,'长','宽'),'parallelogram':(a,h,'底','对应高'),'triangle':(F(a,2),h,'底的一半','对应高'),'cuboid':(a*b,h,'底面积','高'),'cylinder':(pi*a*a,h,'底面积','高'),'cone':(pi*a*a/3,h,'底面积的三分之一','高')}
        if mode in linear:
            base,answer,base_name,missing=linear[mode]
            return q(f'一个图形的{missing}未知，其{base_name}为{fmt(base)}'+('平方厘米' if mode in ('cuboid','cylinder','cone') else '厘米')+f'，其'+('体积' if '立方' in unit else '面积')+f'为{fmt(value)}{unit}，{missing}是多少厘米？',answer,f'({expr})/({base})',f'根据公式，{missing}=测量结果÷{base_name}。',f'{fmt(value)} ÷ {fmt(base)} = {answer}。',mistake,'反向求量','厘米',100)
        return q(f'已算出{stem}是{fmt(value)}{unit}。如果每{unit}计1分，应计多少分？',value,expr,method,work,mistake,'生活应用','分',90)
    if k==3:return judge(f'{stem}是{fmt(value+int(n%3==0))}{unit}',n%3!=0,work,mistake)
    choices=['厘米','平方厘米','立方厘米'];shift=n%3;choices=choices[shift:]+choices[:shift]
    return q(f'计算{stem}时，结果应该用哪个单位？（选0、1或2）',choices.index(unit),str(choices.index(unit)),method,f'这里求的是'+('体积' if '立方' in unit else '面积' if '平方' in unit else '周长')+f'，单位是{unit}。',mistake,'单位辨析',options=choices)


def special(c,k,n):
    mode=c['mode'];a=3+n%7;b=2+n%5
    if mode=='round':
        base=c.get('base',10); number=(a+1)*base+(n*3) % base; rounded=((number+base//2)//base)*base
        if k==3:return judge(f'{number}四舍五入到最接近的{base}的倍数是{rounded}',True,f'看被舍去部分是否达到{base//2}，结果为{rounded}。','四舍五入不是一律舍去。')
        return q([f'{number}四舍五入到最接近的{base}的倍数是多少？',f'把{number}按{base}为单位估计，最接近多少？',f'活动约有{number}人，四舍五入到最接近的{base}的倍数，约多少人？', '',f'{number}四舍五入到最接近的{base}的倍数后，与原数相差多少？'][k],abs(rounded-number) if k==4 else rounded,str(abs(rounded-number) if k==4 else rounded),'看要保留位的下一位，小于5舍去，达到5进一。',f'近似数是{rounded}；与原数相差{abs(rounded-number)}。','近似值要注明取近似的方法。',['四舍五入','估算','生活应用','','误差比较'][k])
    if mode=='remainder':
        divisor=b+2; quotient=a; rem=1+n%(divisor-1);total=divisor*quotient+rem
        if k==3:return judge(f'{total} ÷ {divisor}的余数是{rem+divisor}',False,f'{total} = {divisor} × {quotient} + {rem}，余数应小于除数。','余数必须小于除数。')
        values=[rem,quotient,quotient+1,total,quotient];exprs=[f'{total}%{divisor}',f'{total}//{divisor}',f'{total}//{divisor}+1',str(total),f'{total}//{divisor}']
        texts=[f'{total} ÷ {divisor}的余数是多少？',f'{total} ÷ {divisor}的整数商是多少？',f'{total}人乘车，每车最多坐{divisor}人，至少需要几辆车？','',f'{total}米彩带，每段{divisor}米，最多剪出多少完整段？']
        return q(texts[k],values[k],exprs[k],'先求整数商和余数，再根据情境判断是否需要多一份。',f'{total} = {divisor} × {quotient} + {rem}。'+('有余下的人，还需要1辆车。' if k==2 else ''),'需要全部装下时要进一，只取完整份数时不能进一。',['余数计算','求商','进一应用','','去尾应用'][k])
    if mode=='mixed':
        if k==3:return judge(f'({a} + {b}) × 2 = {a} + {b} × 2',False,f'左边是{(a+b)*2}，右边是{a+b*2}。','有括号先算括号；没有括号先乘除后加减。')
        exprs=[f'{a}+{b}*2',f'({a}+{b})*2',f'{a}*{b}+{a}',str(0),f'{a}*({b}+1)'];vals=[a+b*2,(a+b)*2,a*b+a,0,a*(b+1)]
        texts=[f'{a} + {b} × 2 = ?',f'({a} + {b}) × 2 = ?',f'每盒{a}支笔，买{b}盒再另买{a}支，共多少支？','',f'用简便方法计算{a} × {b} + {a}。']
        return q(texts[k],vals[k],exprs[k],'有括号先算括号；先乘除，后加减。',f'按顺序计算得到{vals[k]}。','不要一律从左向右计算。',['运算顺序','括号计算','生活应用','','简便运算'][k],seconds=75)
    if mode in ('factor','multiple','prime','gcd','lcm'):
        import math
        x=a*b;y=a*(b+1);g=math.gcd(x,y);l=math.lcm(x,y)
        if mode=='gcd': value=g;expr=str(g);stem=f'{x}和{y}的最大公因数';work=f'两个数分别为{a}×{b}和{a}×{b+1}，相邻整数互质，最大公因数为{g}。'
        elif mode=='lcm':value=l;expr=f'{x}*{y}/{g}';stem=f'{x}和{y}的最小公倍数';work=f'先求最大公因数{g}，再算{x}×{y}÷{g}={l}。'
        elif mode=='multiple':value=x;expr=f'{a}*{b}';stem=f'{a}的第{b}个正倍数';work=f'{a}×{b}={x}。'
        elif mode=='prime':
            value=[2,3,5,7,11,13,17,19][n%8];expr=str(value);stem=f'{value}与{value*2}这两个数中，质数';work=f'{value}只有1和它本身两个正因数，而{value*2}还有因数2。'
        else:value=b;expr=f'{x}/{a}';stem=f'{x}的一对因数中，一个是{a}，另一个';work=f'{x}÷{a}={b}。'
        if k==3:return judge(f'{stem}是{fmt(value+1)}',False,work,'因数和倍数的定义不同，1既不是质数也不是合数。')
        if k==4:return q(f'求出{stem}，再加2，结果是多少？',value+2,f'({expr})+2','先按定义求数，再完成第二步。',work+f'再加2得{value+2}。','不能漏掉第二步。','多步应用')
        return q([f'{stem}是多少？',f'填空：{stem}为□。',f'数学卡片上要求找到{stem}，应写哪个数？'][k],value,expr,'按因数、倍数或质数的定义求数。',work,'不要把最大公因数与最小公倍数混淆。',['定义计算','关系填空','生活应用'][k])
    if mode in ('percent','discount','interest','ratio','scale','proportion'):
        total=100*a;rate=[10,20,25,50,75][n%5]
        if mode=='percent':v=F(total*rate,100);expr=f'{total}*{rate}/100';stem=f'{total}的{rate}%';formula='单位“1”的量×百分率';unit=None
        elif mode=='discount':rate=[50,60,70,80,90][n%5];v=F(total*rate,100);expr=f'{total}*{rate}/100';stem=f'原价{total}元，按原价的{rate}%出售的现价';formula='原价×折扣率';unit='元'
        elif mode=='interest':rate=2+n%4;v=F(total*rate,100);expr=f'{total}*{rate}/100';stem=f'本金{total}元，年利率{rate}%，存一年、不计其他费用的利息';formula='本金×年利率×年数';unit='元'
        elif mode=='ratio':v=a;expr=f'{a*b}/{b}';stem=f'比{a*b}:{b}化成后项为1的比，前项';formula='前项与后项同时除以后项';unit=None
        elif mode=='scale':v=a*b;expr=f'{a}*{b}';stem=f'比例尺1:{b}，图上{a}厘米对应的实际距离';formula='实际距离=图上距离÷比例尺';unit='厘米'
        else:v=a*b;expr=f'{a}*{b}';stem=f'每本书{a}元，买{b}本的总价';formula='单价一定，总价与数量成正比例';unit='元'
        work=f'依据{formula}，{expr.replace("*","×")} = {fmt(v)}。'
        if k==3:return judge(f'{stem}是{fmt(v+1)}{unit or ""}',False,work,'百分数要除以100；比例尺计算前先统一单位。')
        if k==1:
            if mode in ('percent','discount','interest'):
                return q(f'一个总量的{rate}%是{fmt(v)}，这个总量是多少？',total,f'({expr})/({rate}/100)','已知部分量和百分率，单位“1”的量=部分量÷百分率。',f'{fmt(v)} ÷ {rate}% = {total}。','求单位“1”时不能再乘百分率。','反向求量')
            return q(f'{stem}是{fmt(v)}。这个数的一半是多少？',v/2,f'({expr})/2',formula,work+f'再除以2得{fmt(v/2)}。','第二步求一半用除以2。','关系填空')
        if k==4:return q(f'{stem}与{fmt(v+5)}相差多少？',5,f'({v+5})-({expr})',formula,work+f'再求差：{fmt(v+5)} − {fmt(v)} = 5。','求的是相差量，不是原来的量。','多步应用',unit)
        return q(f'{stem}是多少{unit or ""}？',v,expr,formula,work,'分清原价、现价、本金、利息、图上距离和实际距离。','生活应用' if k==2 else '直接计算',unit)
    if mode=='average':
        nums=[a,a+2,a+4];mean=a+2
        if k==3:return judge(f'{nums[0]}、{nums[1]}、{nums[2]}的平均数为{mean+1}',False,f'总和{sum(nums)}÷3={mean}。','平均数不是最大数，也不一定是任何一组的实际数。')
        texts=[f'{nums[0]}、{nums[1]}、{nums[2]}的平均数是多少？',f'3个数的平均数为{mean}，它们的和是多少？',f'三天各读{nums[0]}、{nums[1]}、{nums[2]}页，平均每天读几页？','',f'3个数的平均数为{mean}，前两个数为{a}和{a+2}，第三个数是多少？']
        vals=[mean,mean*3,mean,0,a+4];exprs=[f'({a}+{a+2}+{a+4})/3',f'{mean}*3',f'({a}+{a+2}+{a+4})/3','0',f'{mean}*3-{a}-{a+2}']
        return q(texts[k],vals[k],exprs[k],'平均数=总数量÷份数；总数量=平均数×份数。',f'总和{sum(nums)}，除以3得{mean}。','不要把不同份数的数据直接平均。',['平均数计算','反向求总量','生活应用','','补缺数据'][k])
    if mode=='statistics':
        nums=[a,a+b,a+2*b];total=sum(nums)
        if k==3:return judge(f'三组分别有{nums}人，第三组比第一组多{b}人',False,f'{nums[2]}−{nums[0]}={2*b}。','读数据时要对应组别，不要漏看中间的变化。')
        texts=[f'统计表：一组{nums[0]}人、二组{nums[1]}人、三组{nums[2]}人。一共有多少人？',f'统计表：一组{nums[0]}人、二组{nums[1]}人、三组{nums[2]}人。最多的一组有多少人？',f'折线数据依次为{nums[0]}、{nums[1]}、{nums[2]}，从第一次到第三次增加多少？','',f'三组共有{total}人，一组{nums[0]}人、二组{nums[1]}人，三组多少人？']
        vals=[total,nums[2],2*b,0,nums[2]];exprs=[f'{nums[0]}+{nums[1]}+{nums[2]}',str(nums[2]),f'{nums[2]}-{nums[0]}','0',f'{total}-{nums[0]}-{nums[1]}']
        return q(texts[k],vals[k],exprs[k],'先找到对应的数据，再确定求和、比较还是变化。',f'一组{nums[0]}、二组{nums[1]}、三组{nums[2]}，按题意计算得到{vals[k]}。','总数、最大值和增加量是不同的问题。',['读表求和','数据比较','变化趋势','','补全统计表'][k],'人' if k in (0,1,4) else None)
    if mode=='probability':
        if k==3:return judge(f'袋中有{a}个红球和{b}个蓝球，任意摸一个球一定是红球。',False,'袋中还有蓝球，摸出红球是可能的，不是一定的。','一定、可能、不可能含义不同。')
        texts=[f'袋里有{a}个红球、{b}个蓝球，一共多少球？',f'袋里有{a+b}个球，其中红球{a}个，蓝球有多少个？',f'袋里有{a}个红球、{b}个蓝球。要让红球比蓝球至少多1个，最少再放几个红球？','',f'袋里只有{a}个红球和{b}个蓝球，摸到黄球是否可能？（可能选1，不可能选0）']
        v=[a+b,b,max(0,b+1-a),0,0][k];e=[f'{a}+{b}',f'{a+b}-{a}',str(max(0,b+1-a)),'0','0'][k]
        return q(texts[k],v,e,'先明确袋里有哪些球，以及每种球的个数。',f'根据已有球的颜色与数量，答案为{v}。','没有放入的颜色不可能摸到。',['总量计算','补缺数量','可能性比较','','事件判断'][k],options=['不可能','可能'] if k==4 else None)
    if mode in ('clock','elapsed'):
        hour=1+n%10;minute=5*(n%12);duration=10+5*(n%8);end=hour*60+minute+duration
        if k==3:return judge(f'从{hour}时{minute}分到{end//60}时{end%60}分，经过{duration+1}分钟',False,f'结束时刻−开始时刻={duration}分钟。','1小时是60分钟，不是100分钟。')
        texts=[f'{hour}时{minute}分，分针从12开始顺时针转过多少个小格？',f'{hour}小时等于多少分钟？',f'从{hour}时{minute}分到{end//60}时{end%60}分，经过多少分钟？','',f'活动从{hour}时{minute}分开始，持续{duration}分钟。结束时刻从当天0时算起，一共经过多少分钟？']
        vals=[minute,hour*60,duration,0,end];expr=[str(minute),f'{hour}*60',f'{end}-({hour}*60+{minute})','0',f'{hour}*60+{minute}+{duration}']
        return q(texts[k],vals[k],expr[k],'钟面一圈60小格，每大格5分钟；经过时间用结束时刻减开始时刻。',f'先换成分钟，再计算得{vals[k]}。','时刻和经过时间不同。',['钟面读数','时分换算','经过时间','','结束时刻'][k],'分钟' if k!=0 else '格')
    if mode=='calendar':
        year=2020+n
        leap=year%400==0 or year%4==0 and year%100!=0;feb=29 if leap else 28
        if k==3:return judge(f'{year}年2月有{feb}天',True,f'{year}年'+('是闰年' if leap else '是平年')+f'，2月有{feb}天。','整百年份必须能被400整除才是闰年。')
        big=[1,3,5,7,8,10,12][n%7];small=[4,6,9,11][n%4]
        vals=[feb,31,30,0,31+feb];texts=[f'{year}年2月有多少天？',f'一年中的{big}月有多少天？',f'一年中的{small}月有多少天？','',f'{year}年的1月和2月一共多少天？']
        return q(texts[k],vals[k],str(vals[k]),'大月31天、小月30天，2月平年28天、闰年29天。',f'{year}年2月有{feb}天，按月份求和。','不能把所有月份都算成30天。',['平闰年','大月认识','小月认识','','日期应用'][k],'天')
    if mode=='angle':
        angle=10*(1+n%8);other=180-angle
        if k==3:return judge(f'{angle}°的角是直角',False,'直角是90°，这个角小于90°，是锐角。','角的大小与边画得长短无关。')
        texts=[f'平角是180°，分成一个{angle}°的角和另一个角。另一个角是多少度？',f'直角分成{angle}°的角和另一个角，另一个角是多少度？',f'三角形两个角为{angle}°和60°，第三个角是多少度？','',f'一个周角减去{angle}°，剩多少度？'];vals=[other,90-angle,120-angle,0,360-angle];exprs=[f'180-{angle}',f'90-{angle}',f'180-{angle}-60','0',f'360-{angle}']
        return q(texts[k],vals[k],exprs[k],'直角90°，平角180°，周角360°，三角形内角和180°。',f'用总角度减去已知部分，得{vals[k]}°。','不能把平角与周角混淆。',['平角分割','直角分割','三角形内角','','周角认识'][k],'度')
    if mode=='triangle':
        if k==3:return judge(f'长度为{a}、{b}、{a+b}厘米的三条线段能围成三角形',False,'两条较短边的和必须大于第三边；这里相等，不能围成。','三角形两边和必须大于第三边，等于也不行。')
        count=1+n%5;angle=10+5*n
        vals=[180-angle-30,count*3,count*4,0,3*a+1];texts=[f'三角形两个角是{angle}°和30°，第三个角是多少度？',f'{count}个三角形各自的边数之和是多少？',f'{count}个四边形各自的边数之和是多少？','',f'等腰三角形两条腰各{a}厘米，底边{a+1}厘米，周长是多少厘米？']
        return q(texts[k],vals[k],str(vals[k]),'三角形有三条边，内角和180°；周长是各边长度之和。',f'根据题目条件计算得{vals[k]}。','等腰三角形的两条腰相等，不代表底边也相等。',['内角计算','边数识别','图形比较','','周长应用'][k], '度' if k==0 else '厘米' if k==4 else None)
    if mode=='shapes':
        shape,edges,vertices=c.get('shape',('长方体',12,8))
        count=1+n%4
        if k==3:return judge(f'{count}个{shape}各自的'+('棱' if edges==12 else '边')+f'数之和是{edges*count+1}',False,f'每个{shape}有{edges}条，共{edges*count}条。','平面图形的边和立体图形的棱要区分。')
        vals=[edges*count,vertices*count,edges*(count+1),0,vertices*(count+2)];texts=[f'{count}个{shape}各自有几条'+('棱？填写总数。' if edges==12 else '边？填写总数。'),f'{count}个{shape}的顶点数相加是多少？',f'{count+1}个{shape}模型各自的'+('棱' if edges==12 else '边')+'数相加是多少？','',f'用小珠子表示{shape}的顶点，做{count+2}个模型需要几颗珠子？']
        return q(texts[k],vals[k],str(vals[k]),f'{shape}有{edges}条'+('棱' if edges==12 else '边')+f'、{vertices}个顶点。',f'根据模型数量计算，得{vals[k]}。','数顶点时不要重复数同一个点。',['图形特征','顶点识别','模型计数','','生活应用'][k])
    if mode in ('direction','position'):
        row=a;col=b
        if k==3:
            north=n%2==0
            return judge(f'面向'+('北' if north else '南')+'站立时，右手边是西。',not north,'面向北时右手边是东；面向南时右手边是西。','方向要先确定面向哪边。')
        vals=[col+2,row+1,col-1,0,row*col];texts=[f'棋子在第{col}列，向右移动2列后在第几列？',f'座位在第{row}排，向后移动1排，在第几排？',f'棋子在第{col}列，向左移动1列后在第几列？','',f'座位有{row}排，每排{col}个，共多少个座位？'];expr=[f'{col}+2',f'{row}+1',f'{col}-1','0',f'{row}*{col}']
        return q(texts[k],vals[k],expr[k],'约定列从左到右编号、排从前到后编号，移动时只改变对应编号。',f'按移动方向增减，得{vals[k]}。','排和列不能颠倒。',['左右位置','前后位置','反向移动','','位置应用'][k])
    if mode=='pattern':
        step=b;start=a
        if k==3:return judge(f'{start}、{start+step}、{start+2*step}、{start+3*step+1}每次都增加{step}',False,f'最后一次增加{step+1}，规则不一致。','需要检查全部相邻项，不能只看开头。')
        vals=[start+3*step,step,start+4*step,0,start+5*step];texts=[f'找规律：{start}、{start+step}、{start+2*step}、□。',f'数列{start}、{start+step}、{start+2*step}每次增加多少？',f'第一层摆{start}个方块，每上一层增加{step}个，第5层多少个？','',f'从{start}开始，每次增加{step}，增加5次后是多少？'];expr=[f'{start}+3*{step}',str(step),f'{start}+4*{step}','0',f'{start}+5*{step}']
        return q(texts[k],vals[k],expr[k],'先比较相邻项，找出固定增加量。',f'每次增加{step}，按次数计算得{vals[k]}。','第几项与增加了几次相差1。',['规律填空','找出步长','生活应用','','指定项计算'][k])
    if mode=='negative':
        if k==3:return judge(f'−{a}比0大',False,'负数小于0。','负号不是可以忽略的装饰。')
        vals=[-a,a,-a,0,a];texts=[f'温度为零下{a}℃，用带正负号的数表示是多少？',f'零上{a}℃写作多少？',f'−{a}与{a}相比，较小的数是多少？','',f'−{a}与{a}相比，较大的数是多少？']
        return q(texts[k],vals[k],str(vals[k]),'零上用正数，零下用负数；数轴上左边的数较小。',f'按照正负数的意义，答案是{vals[k]}。','两个负数比较，离0更远的反而更小。',['负数表示','正数表示','大小比较','','数轴比较'][k])
    if mode in ('equation','journey','price','plant','chicken','pigeon','optimization'):
        if mode=='equation':answer=a;expr=str(a);stem=f'x + {b} = {a+b}，x';method='等式两边同时减去相同的数，等式仍成立。';work=f'x={a+b}−{b}={a}。'
        elif mode=='journey':answer=a*b;expr=f'{a}*{b}';stem=f'速度{a}千米/时，行驶{b}小时的路程';method='路程=速度×时间。';work=f'{a}×{b}={answer}千米。'
        elif mode=='price':answer=a*b;expr=f'{a}*{b}';stem=f'每本书{a}元，买{b}本的总价';method='总价=单价×数量。';work=f'{a}×{b}={answer}元。'
        elif mode=='plant':answer=a+1;expr=f'{a}+1';stem=f'长{a*b}米的路一侧每隔{b}米种一棵，两端都种，树的棵数';method='两端都种时，棵数=间隔数+1。';work=f'{a*b}÷{b}={a}个间隔，需要{a+1}棵树。'
        elif mode=='chicken':answer=b;expr=f'({2*a+4*b}-2*{a+b})/2';stem=f'鸡兔共{a+b}只，脚共{2*a+4*b}只，兔的只数';method='先假设全是鸡，多出的每2只脚对应1只兔。';work=f'({2*a+4*b}−2×{a+b})÷2={b}只兔。'
        elif mode=='pigeon':answer=3;expr='3';stem=f'把{2*b+1}支笔放进{b}个盒子，至少有一个盒子不少于的支数';method='如果每盒最多2支，最多只能放2×盒子数支。';work=f'2×{b}={2*b}，还剩1支，至少有一个盒子有3支或更多。'
        else:answer=a+2*b;expr=f'{a}+2*{b}';stem=f'洗杯子需{a}分钟、烧水需{2*b}分钟，洗杯子和烧水能同时进行，最后泡茶需{a}分钟；最短总用时';answer=max(a,2*b)+a;expr=f'{max(a,2*b)}+{a}';method='可同时进行的任务取较长时间，再加必须等待后的任务。';work=f'前两项同时做需{max(a,2*b)}分钟，加泡茶{a}分钟，共{answer}分钟。'
        if k==1:
            inverse={
                'equation':(f'2x + {b} = {2*a+b}，x是多少？',a,f'({2*a+b}-{b})/2',f'先减{b}，再除以2，得到x={a}。'),
                'journey':(f'路程{a*b}千米，速度{a}千米/时，需要几小时？',b,f'{a*b}/{a}','时间=路程÷速度。'),
                'price':(f'总价{a*b}元，每本{a}元，买了多少本？',b,f'{a*b}/{a}','数量=总价÷单价。'),
                'plant':(f'一路种{a+1}棵树，两端都种，相邻间隔{b}米，路长多少米？',a*b,f'({a+1}-1)*{b}','间隔数=棵数−1，路长=间隔数×间距。'),
                'chicken':(f'鸡兔共{a+b}只，脚共{2*a+4*b}只，其中鸡有几只？',a,f'{a+b}-({2*a+4*b}-2*{a+b})/2',f'先求兔有{b}只，再用总只数减去兔。'),
                'pigeon':(f'{b}个盒子，要保证至少一盒有3支笔，最少需要放几支笔？',2*b+1,f'2*{b}+1','先让每盒放2支，再多放1支。'),
                'optimization':(f'洗杯子{a}分钟、烧水{2*b}分钟可以同时进行，比依次进行节省几分钟？',min(a,2*b),f'{a}+{2*b}-{max(a,2*b)}','节省时间=两项时间之和−同时进行的时间。')}
            text,val,calc,how=inverse[mode]
            return q(text,val,calc,how,f'按数量关系计算得{val}。','反向求量时要改变列式，不能照抄原公式。','反向求量',seconds=100)
        if k==2:
            applications={
                'equation':(f'小林原有一些书，又买{b}本后共有{a+b}本，原有多少本？',a,f'{a+b}-{b}'),
                'journey':(f'两辆车每小时各走{a}千米和{b}千米，同时从同一地点反向出发，2小时后相距多少千米？',2*(a+b),f'({a}+{b})*2'),
                'price':(f'带{a*b+5}元买{b}本书，每本{a}元，找回多少元？',5,f'{a*b+5}-{a}*{b}'),
                'plant':(f'路长{a*b}米，每隔{b}米种一棵，两端都不种，共种几棵？',a-1,f'{a*b}/{b}-1'),
                'chicken':(f'有{a}只鸡和{b}只兔，共有几只脚？',2*a+4*b,f'2*{a}+4*{b}'),
                'pigeon':(f'把{3*b+1}支笔放进{b}个盒子，至少一盒有几支或更多？',4,'4'),
                'optimization':(f'洗杯子{a}分钟、烧水{2*b}分钟、泡茶{a}分钟。如果三项必须依次进行，总共几分钟？',2*a+2*b,f'{a}+{2*b}+{a}')}
            text,val,calc=applications[mode]
            return q(text,val,calc,'看清变化后的条件，再列出相应数量关系。',f'根据条件，{calc.replace("*","×")}={val}。','注意同时或依次、两端种或不种等条件变化。','生活应用',seconds=100)
        if k==3:return judge(f'{stem}是{answer+1}',False,work,'先明确条件，特别注意是否同时进行、是否两端都种。')
        if k==4:return q(f'{stem}是多少？再把所得数加2，最终是多少？',answer+2,f'({expr})+2',method,work+f'再加2，得{answer+2}。','要完成题目要求的全部步骤。','多步应用',seconds=120)
        return q(f'{stem}是多少？',answer,expr,method,work,'根据数量关系列式，不要只把出现的数字相加。',['数量关系','关系填空','生活应用'][k],seconds=120 if mode in ('chicken','pigeon','optimization') else 75)
    raise ValueError(mode)


def concept(c,k,n):
    mode=c['mode'];a=2+n%8;b=2+n%5;count=2+n%6
    mistake='先理解概念和题目条件，再计算；用结果代回原条件检查。'
    if mode=='decimal-properties':
        x=F(10*a+b,10);y=F(10*a+b,100)
        rows=[(f'{fmt(x)}扩大到原来的10倍是多少？',x*10,f'({x})*10','小数点右移一位，数扩大到原来的10倍。','小数点移动'),
              (f'{fmt(x)}缩小到原来的十分之一是多少？',x/10,f'({x})/10','小数点左移一位，数缩小到原来的十分之一。','反向变化'),
              (f'{fmt(x)}与{fmt(y)}相比，大数是小数的几倍？',10,f'({x})/({y})','两个数的数字相同，但小数点位置相差一位。','倍数比较'),
              (f'{fmt(x)}末尾添一个0，数值是否改变？（改变选1，不变选0）',0,'0','小数末尾添0或去0，大小不变。','性质判断'),
              (f'每个物品重{fmt(y)}千克，10个共重多少千克？',y*10,f'({y})*10','小数乘10，相当于小数点右移一位。','生活应用')]
    elif mode=='fraction-decimal':
        d=[2,4,5,8,10,20,25,50][n%8];x=F(a,d)
        rows=[(f'把{a}/{d}化为小数。',x,f'{a}/{d}','分数化小数，用分子除以分母。','分数化小数'),
              (f'把{fmt(x)}化为最简分数。',x,str(x),'按小数位数写成分母为10、100等的分数，再约分。','小数化分数'),
              (f'{a}/{d}和{fmt(x)}相等吗？（相等选1，不等选0）',1,'1',f'{a}÷{d}={fmt(x)}。','等值判断'),
              (f'{a}/{d}米彩带和{fmt(x)}米彩带合起来长多少米？',2*x,f'2*({x})','先统一表示方法，再相加。','生活应用'),
              (f'□ × {d} = {a}，□是多少？',x,f'{a}/{d}','把分数看成除法，未知数=积÷已知因数。','逆运算填空')]
    elif mode=='decimal-round':
        x=F(100*a+10*b+1+n%8,100);rounded=F((x*10+F(1,2)).numerator//(x*10+F(1,2)).denominator,10)
        rows=[(f'{fmt(x)}保留一位小数是多少？',rounded,str(rounded),'看百分位，四舍五入到十分位。','小数近似值'),
              (f'{fmt(x)}保留整数是多少？',a+int(b>=5),str(a+int(b>=5)),'看十分位，四舍五入到个位。','不同精度'),
              (f'{fmt(x)}元按四舍五入保留到角，是多少元？',rounded,str(rounded),'元的一位小数对应角。','生活应用'),
              (f'{fmt(x)}保留一位小数是{fmt(rounded+F(1,10))}，正确吗？（正确1，错误0）',0,'0',f'正确近似值是{fmt(rounded)}。','判断辨析'),
              (f'{fmt(x)}与它保留一位小数后的近似值相差多少？',abs(x-rounded),str(abs(x-rounded)),'较大数减较小数，求近似误差。','误差比较')]
    elif mode=='divisibility':
        even=2*(10+a);five=5*(a+1);three=3*(a+1)
        rows=[(f'{even}与{even+1}中，2的倍数是多少？',even,str(even),'个位是0、2、4、6、8的整数是2的倍数。','2的倍数'),
              (f'{five}与{five+1}中，5的倍数是多少？',five,str(five),'个位是0或5的整数是5的倍数。','5的倍数'),
              (f'{three}÷3的商是多少？',a+1,f'{three}/3','各位数字之和是3的倍数的整数是3的倍数。','3的倍数'),
              (f'{even+1}是偶数吗？（是1，不是0）',0,'0','个位不是0、2、4、6、8，是奇数。','奇偶判断'),
              (f'有{even}支笔，每2支装一袋，可以装几袋？',even//2,f'{even}/2','按每份2支平均分。','生活应用')]
    elif mode=='reciprocal':
        x=F(a,b)
        rows=[(f'{fraction(x)}的倒数是多少？',1/x,f'1/({x})','乘积是1的两个数互为倒数。','求倒数'),
              (f'{a}的倒数是多少？',F(1,a),f'1/{a}','整数可看成分母为1的分数。','整数倒数'),
              (f'{fraction(x)} × □ = 1，□是多少？',1/x,f'1/({x})','未知因数就是已知分数的倒数。','乘积填空'),
              (f'{fraction(x)}的倒数是{fraction(x+1)}吗？（是1，不是0）',int(x*(x+1)==1),str(int(x*(x+1)==1)),f'正确倒数是{fraction(1/x)}。','判断辨析'),
              (f'一段彩带长1米，每段长1/{a}米，可以剪几段？',a,f'1/(1/{a})','除以分数等于乘它的倒数。','生活应用')]
    elif mode=='ratio-allocation':
        total=(a+b)*count
        rows=[(f'{total}颗珠子按{a}:{b}分给两人，第一人得几颗？',a*count,f'{total}*{a}/({a}+{b})','总份数=比的前后项之和，每份数量=总量÷总份数。','按比分配'),
              (f'{total}颗珠子按{a}:{b}分，第二人得几颗？',b*count,f'{total}*{b}/({a}+{b})','第二人占总份数中的后项份数。','求另一部分'),
              (f'两人数量比为{a}:{b}，第一人有{a*count}颗，两人共有几颗？',total,f'{a*count}/{a}*({a}+{b})','先求每份数量，再求总份数对应的数量。','反向求总量'),
              (f'按{a}:{b}分配{total}，第一人得到{a*count+1}，正确吗？（正确1，错误0）',0,'0',f'第一人应得{a*count}。','判断辨析'),
              (f'{total}元按{a}:{b}分配，两人所得相差几元？',abs(a-b)*count,f'{abs(a-b)}*{count}','相差的份数×每份数量=相差的数量。','生活应用')]
    elif mode=='inverse-proportion':
        total=a*b*count
        rows=[(f'总路程{total}千米，以每小时{a}千米行驶，需要几小时？',b*count,f'{total}/{a}','路程一定，速度×时间一定。','反比例计算'),
              (f'总路程{total}千米，要在{b*count}小时内走完，速度是多少千米/时？',a,f'{total}/{b*count}','速度=路程÷时间。','反向求量'),
              (f'走同一段路，速度变成原来的2倍，原需{2*b}小时，现在需几小时？',b,f'{2*b}/2','路程不变，速度加倍，时间减半。','倍数变化'),
              (f'路程一定，速度和时间成正比例吗？（是1，不是0）',0,'0','速度与时间的乘积一定，成反比例。','正反比例辨析'),
              (f'{total}本书，每箱装{a}本，需要几箱？',b*count,f'{total}/{a}','总量一定，每箱数量×箱数一定。','生活应用')]
    elif mode=='combinatorics':
        rows=[(f'{a}件上衣、{b}条裤子，每次各选1件，共有几种搭配？',a*b,f'{a}*{b}','每件上衣都可搭配每条裤子。','搭配计数'),
              (f'{a}个人，每两人握一次手，共握几次？',a*(a-1)//2,f'{a}*({a}-1)/2','每人可和其他人握手，但每次被数了两遍。','组合计数'),
              (f'用{a}张不同数字卡选两张排成两位数（都不是0），共有几个？',a*(a-1),f'{a}*({a}-1)','十位有a种选法，个位不能重复选，少一种。','排列计数'),
              (f'{a}件上衣、{b}条裤子，共有{a+b}种搭配，正确吗？（正确1，错误0）',int(a*b==a+b),str(int(a*b==a+b)),f'应算{a}×{b}={a*b}。','判断辨析'),
              (f'{a}种主食、{b}种配菜，各选一种，每套餐价格5元。共有几种不同套餐？',a*b,f'{a}*{b}','价格不是求搭配数所需条件。','生活应用')]
    elif mode=='sets':
        x=a+3;y=b+3;both=min(a,b);total=x+y-both
        rows=[(f'{x}人喜欢足球，{y}人喜欢篮球，其中{both}人两项都喜欢，至少喜欢一项的有几人？',total,f'{x}+{y}-{both}','两组人数相加后，重复部分要减去一次。','集合求总量'),
              (f'{x}人喜欢足球，其中{both}人也喜欢篮球，只喜欢足球的有几人？',x-both,f'{x}-{both}','从一组中减去两组共有的部分。','求单独部分'),
              (f'喜欢足球或篮球的共{total}人，足球{x}人、篮球{y}人，两项都喜欢的几人？',both,f'{x}+{y}-{total}','重复人数=两组之和−合并总人数。','反向求重叠'),
              (f'{x}人喜欢足球，{y}人喜欢篮球，有{both}人重复，总人数是{x+y}，正确吗？（正确1，错误0）',0,'0',f'重复的{both}人多算了一次，正确为{total}。','判断辨析'),
              (f'班级{total+2}人，喜欢足球或篮球的共{total}人，两项都不喜欢的几人？',2,f'{total+2}-{total}','全班人数减去至少喜欢一项的人数。','生活应用')]
    elif mode in ('symmetry','rotation','views','right-angle'):
        if mode=='right-angle':
            rows=[(f'{count}张长方形卡片共有几个直角？',4*count,f'4*{count}','长方形的四个角都是直角。','直角计数'),
                  (f'{count}个正方形共有几个直角？',4*count,f'4*{count}','正方形也有四个直角。','图形辨认'),
                  (f'一张长方形纸的4个角中，检查了{min(b,3)}个，还有几个角没检查？',4-min(b,3),f'4-{min(b,3)}','检查角时不能漏数或重复。','补缺计数'),
                  (f'画{count}个角时，把边画长，角就一定变大吗？（是1，不是0）',0,'0','角的大小取决于两条边张开的程度。','判断辨析'),
                  (f'拼图需要{4*count}个直角，每张正方形纸提供4个，需要几张？',count,f'{4*count}/4','每张有4个直角，用总数除以4。','生活应用')]
        elif mode=='symmetry':
            rows=[(f'{count}个非正方形长方形各自有几条对称轴？填写总数。',2*count,f'2*{count}','非正方形长方形有2条对称轴。','对称轴计数'),
                  (f'{count}个正方形的对称轴数量之和是多少？',4*count,f'4*{count}','正方形有4条对称轴。','图形比较'),
                  (f'对称轴左右一对点，左点离轴{a}厘米，右点离轴几厘米？',a,str(a),'对应点到对称轴的距离相等。','对应点距离'),
                  (f'一个非正方形长方形的{count}倍放大图形有4条对称轴吗？（是1，不是0）',0,'0','放大不改变形状，仍有2条对称轴。','判断辨析'),
                  (f'镜面对称的一对点分别离对称轴{a}厘米，两点在垂直于轴的同一直线上，相距几厘米？',2*a,f'2*{a}','两侧距离相加。','生活应用')]
        elif mode=='rotation':
            turns=1+n%3
            rows=[(f'图形每次旋转90°，旋转{turns}次共转多少度？',90*turns,f'90*{turns}','旋转角度=每次角度×次数。','旋转角度'),
                  (f'指针顺时针旋转{90*turns}°，相当于几次90°？',turns,f'{90*turns}/90','用总角度除以每次角度。','反向求次数'),
                  (f'一个周角是360°，已旋转{90*turns}°，还差多少度？',360-90*turns,f'360-90*{turns}','一周减去已旋转角度。','补全旋转'),
                  (f'图形旋转{90*turns}°后，面积一定改变吗？（改变1，不变0）',0,'0','旋转只改变位置和方向，不改变大小。','判断辨析'),
                  (f'正方形转盘每转90°经过一个标记，转{turns}次共经过几次标记？',turns,str(turns),'按转动次数计数，不把出发标记计入。','生活应用')]
        else:
            rows=[(f'正方体从正前方看是正方形。{count}个互不遮挡的正方体，每个投影各有4条边，边数之和多少？',4*count,f'4*{count}','正方体的正面、侧面和上面都是正方形。','视图特征'),
                  (f'{count}个正方体，每个分别画正面、侧面、上面三张图，共画几张？',3*count,f'3*{count}','每个物体观察三个方向。','观察方向'),
                  (f'把{count}个正方体并排放一行，从正前方看有几列小正方形？',count,str(count),'每个正方体占一列，互不重叠。','正面计数'),
                  (f'{count}个正方体从不同方向观察，每个看到的形状一定完全不同吗？（是1，不是0）',0,'0','正方体从三个正对的方向看，形状都可以是正方形。','判断辨析'),
                  (f'两行积木，每行{count}个，俯视每个占一个方格，一共有几格？',2*count,f'2*{count}','俯视按行数×每行个数计数。','俯视应用')]
    elif mode=='tax':
        total=100*a;rate=2+n%4;tax=F(total*rate,100)
        rows=[(f'按{rate}%税率对{total}元计税，税额是多少元？',tax,f'{total}*{rate}/100','税额=计税金额×税率。','税额计算'),
              (f'按{rate}%税率缴税{fmt(tax)}元，计税金额是多少元？',total,f'({tax})/({rate}/100)','计税金额=税额÷税率。','反向求量'),
              (f'收入{total}元，按{rate}%缴税，不计其他扣除，税后剩多少元？',total-tax,f'{total}-({tax})','税后金额=收入−税额。','生活应用'),
              (f'{total}元的{rate}%是{fmt(tax+1)}元，正确吗？（正确1，错误0）',0,'0',f'正确税额是{fmt(tax)}元。','判断辨析'),
              (f'两笔同为{total}元的收入，各按{rate}%计税，税额合计几元？',2*tax,f'2*({tax})','先算一笔税额，再求两笔总额。','多步应用')]
    elif mode=='percent-change':
        total=100*a;rate=10*b;change=F(total*rate,100)
        rows=[(f'{total}增加{rate}%后是多少？',total+change,f'{total}*(1+{rate}/100)','增加后的量=原量×（1＋增长率）。','增长计算'),
              (f'{total}减少{rate}%后是多少？',total-change,f'{total}*(1-{rate}/100)','减少后的量=原量×（1−减少率）。','减少计算'),
              (f'增加{rate}%后为{fmt(total+change)}，原量是多少？',total,f'({total+change})/(1+{rate}/100)','原量=增加后的量÷（1＋增长率）。','反向求量'),
              (f'{total}增加{rate}%就是加{rate}，正确吗？（正确1，错误0）',0,'0',f'增加的是{fmt(change)}，不是{rate}。','判断辨析'),
              (f'商品原价{total}元，涨价{rate}%后，比原价贵多少元？',change,f'{total}*{rate}/100','增加量=原价×增长率。','生活应用')]
    elif mode=='puzzle':
        # Equal-weight counterfeit: deliberately state whether the odd item is lighter.
        total=3**(1+n%3);weighings=1+n%3
        rows=[(f'{total}个外观相同物品中有1个较轻。每次分成尽量相等的3组用天平比较，至少几次能保证找出？',weighings,str(weighings),'每次称量最多区分左轻、右轻、平衡三种结果。','找次品'),
              (f'称{weighings}次，每次最多区分3种结果，最多能区分几个候选物品？',total,f'3**{weighings}','最多候选数是3的称量次数次方。','反向容量'),
              (f'{total}个候选物品分成相等的3组，每组几个？',total//3,f'{total}/3','分成三等份。','分组策略'),
              (f'{total}个物品中有1个轻物品，用{weighings}次三分法可保证找到吗？（可以1，不可以0）',1,'1',f'3的{weighings}次方是{total}。','判断辨析'),
              (f'{total}个糖盒分成3组，一组拿走后，还剩几个？',total-total//3,f'{total}-{total}/3','先求每组数量，再减去一组。','生活应用')]
    else:raise ValueError(mode)
    text,value,expr,method,kind=rows[k]
    options=None
    if kind in ('判断辨析','性质判断','奇偶判断','正反比例辨析','等值判断'):
        options=['错误','正确'] if '正确' in text else ['不变','改变'] if '改变' in text else ['不等','相等'] if '相等' in text else ['不是','是']
    return q(text,value,expr,method,f'依据题目条件计算或判断，答案为{fmt(value)}。',mistake,kind,seconds=90,options=options)


SPECS=[]
def add(g,s,unit,name,family,**config):
    SPECS.append(dict(grade=g,semester=s,unit=unit,name=name,family=family,config=config))


# Curriculum organization follows the commonly used PEP primary sequence and
# the selected video-course subjects; it is not a claim of a 2026 edition audit.
add(1,0,'准备与认识数','1～5的认识与组成','numbers',limit=5)
add(1,0,'位置','前后左右与位置','special',mode='position')
add(1,0,'10以内的数与运算','6～10的认识与比较','numbers',limit=10)
add(1,0,'10以内的数与运算','10以内的加法','arithmetic',op='+',limit=10)
add(1,0,'10以内的数与运算','10以内的减法','arithmetic',op='-',limit=10)
add(1,0,'认识图形','平面图形的边与顶点','special',mode='shapes',shape=('三角形',3,3))
add(1,0,'11～20各数的认识','十与一及数的组成','numbers',limit=20,place=10)
add(1,0,'认识钟表','整时与分钟的初步认识','special',mode='clock')
add(1,0,'20以内的加法','20以内进位加法','arithmetic',op='+',limit=20,carry=True)
add(1,1,'认识图形','长方形与正方形的特征','special',mode='shapes',shape=('长方形',4,4))
add(1,1,'20以内的减法','20以内退位减法','arithmetic',op='-',limit=20,borrow=True)
add(1,1,'分类与整理','分类计数与简单统计','special',mode='statistics')
add(1,1,'100以内数的认识','100以内数的组成与数位','numbers',limit=100,place=10)
add(1,1,'认识人民币','元与角','units',large='元',small='角',factor=10)
add(1,1,'认识人民币','角与分','units',large='角',small='分',factor=10)
add(1,1,'100以内的加减法','两位数加一位数','arithmetic',op='+',limit=100,digits=(10,89,1,9))
add(1,1,'100以内的加减法','两位数减一位数','arithmetic',op='-',limit=100,digits=(11,99,1,9))
add(1,1,'找规律','数列与图案数量规律','special',mode='pattern')
add(2,0,'长度单位','米与厘米','units',large='米',small='厘米',factor=100)
add(2,0,'100以内的加减法','两位数加两位数','arithmetic',op='+',limit=100,digits=(10,49,10,49))
add(2,0,'100以内的加减法','两位数减两位数','arithmetic',op='-',limit=100,digits=(50,99,10,49))
add(2,0,'角的初步认识','平面图形的边与顶点','special',mode='shapes',shape=('正方形',4,4))
add(2,0,'表内乘法','乘法的意义与口诀','arithmetic',op='*',limit=30,table=True)
add(2,0,'观察物体','长方体的棱与顶点','special',mode='shapes')
add(2,0,'认识时间','时与分及经过时间','special',mode='elapsed')
add(2,0,'数学广角','简单排列与有序计数','special',mode='pattern')
add(2,1,'数据收集整理','统计表与数量比较','special',mode='statistics')
add(2,1,'表内除法','平均分与除法口诀','arithmetic',op='/',limit=30,table=True)
add(2,1,'混合运算','乘除加减与小括号','special',mode='mixed')
add(2,1,'有余数的除法','商、余数与进一去尾','special',mode='remainder')
add(2,1,'万以内数的认识','千位与数的组成','numbers',limit=10000,place=1000)
add(2,1,'克和千克','质量单位换算','units',large='千克',small='克',factor=1000)
add(2,1,'数学广角','等差规律与推理','special',mode='pattern')
add(3,0,'时分秒','分与秒','units',large='分钟',small='秒',factor=60)
add(3,0,'时分秒','经过时间','special',mode='elapsed')
add(3,0,'万以内加减法','三位数加法','arithmetic',op='+',limit=1000,digits=(100,499,100,499))
add(3,0,'万以内加减法','三位数减法','arithmetic',op='-',limit=1000,digits=(500,999,100,499))
add(3,0,'测量','厘米与毫米','units',large='厘米',small='毫米',factor=10)
add(3,0,'测量','千米与米','units',large='千米',small='米',factor=1000)
add(3,0,'测量','吨与千克','units',large='吨',small='千克',factor=1000)
add(3,0,'倍的认识','几倍与相同数量相加','arithmetic',op='*',limit=40)
add(3,0,'多位数乘一位数','乘法计算与应用','arithmetic',op='*',limit=200,digits=(10,199,2,9))
add(3,0,'长方形和正方形','长方形周长','geometry',mode='perimeter')
add(3,0,'长方形和正方形','正方形周长','geometry',mode='square-perimeter')
add(3,0,'分数的初步认识','平均分与分数意义','fractions',mode='meaning')
add(3,0,'分数的初步认识','同分母分数加法','fractions',mode='same')
add(3,1,'位置与方向','方向和位置移动','special',mode='direction')
add(3,1,'除数是一位数的除法','整除计算与应用','arithmetic',op='/',limit=150)
add(3,1,'复式统计表','读表、比较与补全','special',mode='statistics')
add(3,1,'两位数乘两位数','乘法与多步应用','arithmetic',op='*',limit=200,digits=(10,99,10,99))
add(3,1,'面积','长方形面积与反求边长','geometry',mode='rectangle')
add(3,1,'面积','正方形面积','geometry',mode='square')
add(3,1,'面积','平方米与平方分米','units',large='平方米',small='平方分米',factor=100)
add(3,1,'面积','平方分米与平方厘米','units',large='平方分米',small='平方厘米',factor=100)
add(3,1,'年、月、日','大小月、平年与闰年','special',mode='calendar')
add(3,1,'小数的初步认识','一位小数加法','arithmetic',op='+',limit=40,decimal=True)
add(3,1,'小数的初步认识','一位小数减法','arithmetic',op='-',limit=40,decimal=True)
add(4,0,'大数的认识','万位与大数的组成','numbers',limit=100000,place=10000)
add(4,0,'大数的认识','亿位与大数的组成','numbers',limit=1000000000,place=100000000)
add(4,0,'大数的认识','四舍五入与近似数','special',mode='round',base=1000)
add(4,0,'公顷和平方千米','公顷与平方米','units',large='公顷',small='平方米',factor=10000)
add(4,0,'公顷和平方千米','平方千米与公顷','units',large='平方千米',small='公顷',factor=100)
add(4,0,'角的度量','角度计算与三角形内角','special',mode='angle')
add(4,0,'三位数乘两位数','乘法笔算与应用','arithmetic',op='*',limit=1000,digits=(100,999,10,99))
add(4,0,'平行四边形和梯形','平面图形的特征','special',mode='shapes',shape=('梯形',4,4))
add(4,0,'除数是两位数的除法','整除笔算与应用','arithmetic',op='/',limit=500,two_digit_divisor=True)
add(4,0,'条形统计图','数据读取、比较与求和','special',mode='statistics')
add(4,0,'数学广角','合理安排时间','special',mode='optimization')
add(4,1,'四则运算','带括号的四则运算','special',mode='mixed')
add(4,1,'运算定律','分配律与简便计算','special',mode='mixed')
add(4,1,'小数意义和性质','小数加法与数位对齐','arithmetic',op='+',limit=100,decimal=True)
add(4,1,'小数加减法','小数减法与退位','arithmetic',op='-',limit=100,decimal=True)
add(4,1,'三角形','特征、内角和与周长','special',mode='triangle')
add(4,1,'平均数与条形统计图','平均数及反求数据','special',mode='average')
add(4,1,'数学广角','鸡兔同笼','special',mode='chicken')
add(5,0,'小数乘法','小数乘整数及小数','arithmetic',op='*',limit=120,decimal=True)
add(5,0,'位置','列、排与位置变化','special',mode='position')
add(5,0,'小数除法','小数除法与实际应用','arithmetic',op='/',limit=120,decimal=True)
add(5,0,'可能性','一定、可能与不可能','special',mode='probability')
add(5,0,'简易方程','解方程与等式性质','special',mode='equation')
add(5,0,'多边形的面积','平行四边形面积','geometry',mode='parallelogram')
add(5,0,'多边形的面积','三角形面积','geometry',mode='triangle')
add(5,0,'多边形的面积','梯形面积','geometry',mode='trapezoid')
add(5,0,'多边形的面积','组合图形面积','geometry',mode='composite')
add(5,0,'数学广角','植树问题','special',mode='plant')
add(5,1,'因数与倍数','因数与倍数关系','special',mode='factor')
add(5,1,'因数与倍数','倍数与指定倍数','special',mode='multiple')
add(5,1,'因数与倍数','质数与合数','special',mode='prime')
add(5,1,'长方体和正方体','长方体表面积','geometry',mode='surface')
add(5,1,'长方体和正方体','正方体表面积','geometry',mode='cube-surface')
add(5,1,'长方体和正方体','长方体体积','geometry',mode='cuboid')
add(5,1,'长方体和正方体','正方体体积','geometry',mode='cube')
add(5,1,'长方体和正方体','立方分米与立方厘米','units',large='立方分米',small='立方厘米',factor=1000)
add(5,1,'长方体和正方体','升与毫升','units',large='升',small='毫升',factor=1000)
add(5,1,'分数的意义和性质','分数意义与等值分数','fractions',mode='equivalent')
add(5,1,'分数的意义和性质','约分与分数比较','fractions',mode='simplify')
add(5,1,'分数的意义和性质','最大公因数','special',mode='gcd')
add(5,1,'分数的意义和性质','最小公倍数','special',mode='lcm')
add(5,1,'分数加减法','异分母分数加法','fractions',mode='add')
add(5,1,'分数加减法','异分母分数减法','fractions',mode='sub')
add(5,1,'折线统计图','变化趋势与统计数据','special',mode='statistics')
add(6,0,'分数乘法','分数乘法及应用','fractions',mode='mul')
add(6,0,'位置与方向','方位与位置','special',mode='direction')
add(6,0,'分数除法','分数除法及应用','fractions',mode='div')
add(6,0,'比','比的化简与意义','special',mode='ratio')
add(6,0,'圆','圆的周长','geometry',mode='circle-perimeter')
add(6,0,'圆','圆的面积','geometry',mode='circle')
add(6,0,'百分数','求一个数的百分之几','special',mode='percent')
add(6,0,'统计与规律','统计数据与数量关系','special',mode='statistics')
add(6,1,'负数','负数意义与大小比较','special',mode='negative')
add(6,1,'百分数应用','折扣与现价','special',mode='discount')
add(6,1,'百分数应用','本金、利率与利息','special',mode='interest')
add(6,1,'圆柱与圆锥','圆柱表面积','geometry',mode='cylinder-surface')
add(6,1,'圆柱与圆锥','圆柱体积','geometry',mode='cylinder')
add(6,1,'圆柱与圆锥','圆锥体积','geometry',mode='cone')
add(6,1,'比例','比例与正比例数量关系','special',mode='proportion')
add(6,1,'比例','比例尺与距离','special',mode='scale')
add(6,1,'数学广角','抽屉原理','special',mode='pigeon')
add(6,1,'整理与复习','路程、速度与时间','special',mode='journey')
add(6,1,'整理与复习','单价、数量与总价','special',mode='price')
add(6,1,'整理与复习','平均数与统计复习','special',mode='average')

# Additional concepts with distinct rules, rather than renaming generic drills.
add(2,0,'角的初步认识','直角特征与角的大小','concept',mode='right-angle')
add(2,0,'观察物体','从不同方向观察正方体','concept',mode='views')
add(2,0,'数学广角','搭配、排列与组合','concept',mode='combinatorics')
add(2,1,'图形的运动','轴对称与对应点','concept',mode='symmetry')
add(3,0,'数学广角','集合与重叠计数','concept',mode='sets')
add(4,1,'小数意义和性质','小数性质与小数点移动','concept',mode='decimal-properties')
add(4,1,'小数意义和性质','小数近似值','concept',mode='decimal-round')
add(4,1,'图形的运动','轴对称图形与对称轴','concept',mode='symmetry')
add(5,1,'因数与倍数','2、3、5的倍数与奇偶数','concept',mode='divisibility')
add(5,1,'分数的意义和性质','分数与小数互化','concept',mode='fraction-decimal')
add(5,1,'图形的运动','旋转与大小不变','concept',mode='rotation')
add(5,1,'数学广角','找次品与三分策略','concept',mode='puzzle')
add(6,0,'分数除法','倒数与分数除法关系','concept',mode='reciprocal')
add(6,0,'比','按比分配','concept',mode='ratio-allocation')
add(6,0,'百分数','百分数增减及反向求量','concept',mode='percent-change')
add(6,1,'百分数应用','税率与税额','concept',mode='tax')
add(6,1,'比例','反比例数量关系','concept',mode='inverse-proportion')


def evaluate(expression):
    """Independent exact evaluation of audit expressions; never execute input."""
    def walk(node):
        if isinstance(node,ast.Constant) and isinstance(node.value,int):return F(node.value)
        if isinstance(node,ast.UnaryOp) and isinstance(node.op,ast.USub):return -walk(node.operand)
        if isinstance(node,ast.BinOp):
            a,b=walk(node.left),walk(node.right)
            if isinstance(node.op,ast.Add):return a+b
            if isinstance(node.op,ast.Sub):return a-b
            if isinstance(node.op,ast.Mult):return a*b
            if isinstance(node.op,ast.Div):return a/b
            if isinstance(node.op,ast.FloorDiv):return F(a//b)
            if isinstance(node.op,ast.Mod):return a%b
            if isinstance(node.op,ast.Pow) and b.denominator==1 and 0<=b<=3:return a**int(b)
        raise ValueError(expression)
    return walk(ast.parse(expression,mode='eval').body)


def build():
    topics=[]; names=set(); ids=set(); functions={'arithmetic':arithmetic,'numbers':numbers,'units':units,'fractions':fractions,'geometry':geometry,'special':special,'concept':concept}
    for index,spec in enumerate(SPECS):
        g=spec['grade'];s=spec['semester'];topic_id=f'pep-g{g}-s{s+1}-{index+1:03d}'
        batches=[]
        for review in (False,True):
            questions=[]
            for variant in range(8):
                for kind in range(5):
                    n=variant*2+int(review)+1
                    question=functions[spec['family']](spec['config'],kind,n)
                    question.update(Id=f'{topic_id}-{"r" if review else "q"}{variant*5+kind+1:02d}',Grade=g,TopicId=topic_id,TemplateId=f'{topic_id}-t{kind+1}')
                    assert F(question['Answer'])==evaluate(question['Calculation']),question
                    assert question['Id'] not in ids;ids.add(question['Id'])
                    assert question['Hints'] and len(question['Solution'])>=3 and question['CommonMistake']
                    questions.append(question)
            batches.append(questions)
        # Remove repeated mathematical prompts rather than padding the advertised
        # question count with the same problem under different IDs.
        unique=[[],[]];seen=set()
        for main,review in zip(*batches):
            keys=[(q['Text'],q['Answer']) for q in (main,review)]
            if keys[0]==keys[1] or any(key in seen for key in keys):continue
            seen.update(keys);unique[0].append(main);unique[1].append(review)
        batches=unique
        first=batches[0][0];name=spec['name']; assert (g,s,name) not in names; names.add((g,s,name))
        topic=dict(Id=topic_id,Grade=g,Semester=s,UnitName=spec['unit'],Order=index,Name=name,Area='图形与几何' if spec['family']=='geometry' else '统计与概率' if spec['config'].get('mode') in ('statistics','average','probability') else '数与运算及应用',
                   Goal=f'理解{name}，能完成基础计算、关系辨析及相关应用。',
                   Explanation=first['Hints'][0]+' '+first['CommonMistake'],Visual=first['Hints'][1],ExampleSteps=first['Solution'],Questions=batches[0],ReviewQuestions=batches[1])
        assert len({x['QuestionType'] for x in batches[0]})>=4,(name,[x['QuestionType'] for x in batches[0]])
        assert all(a['Text']!=b['Text'] or a['Answer']!=b['Answer'] for a,b in zip(*batches)),name
        topics.append(topic)
    return topics


def main():
    parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');args=parser.parse_args()
    topics=build();payload=json.dumps(topics,ensure_ascii=False,indent=2)+'\n'
    path=ROOT/'src/MathPet.Core/Content/question-bank.json'
    report=['# 原创小学数学题型覆盖清单','','由 `scripts/generate-question-bank.py` 生成。题干、提示和解析独立编写，不复制教材或网站题目。',
            '',f'新增 {len(topics)} 个知识点，每个知识点至少4类题型，共 {sum(len(t["Questions"])*2 for t in topics)} 道参数化题目（含对应相似题）。已去除同知识点内重复题干与答案。另保留原有6个样例和36道题以兼容存档。',
            '', '**范围说明：** 按常用人教版小学知识范围及已选课程进行编排。它不是已核定的2026新版十二册目录；部分教材主题仅有初步题型，缺项见文末。题数包含同模板不同参数，不代表同等数量的独立题型。',
            '', '|册次|单元|知识点|题型|练习 / 相似题|','|---|---|---|---|---|']
    for t in topics:
        report.append(f'|{t["Grade"]}{"上" if t["Semester"]==0 else "下"}|{t["UnitName"]}|{t["Name"]}|'+ '、'.join(dict.fromkeys(q['QuestionType'] for q in t['Questions']))+f'|{len(t["Questions"])} / {len(t["ReviewQuestions"])}|')
    report+=['','## 尚需补充的独立题型','','- 真实图形交互：量角、画图、轴对称、旋转、观察物体视图、钟面指针、统计图及扇形图。当前以明确的文字条件和数字作答为主。',
             '- 部分主题需进一步细分：分类的实际操作、扇形统计图、圆环面积、正反比例图像、具体角度和距离确定方向、综合实践。当前标题不能替代这些内容的完整验收。',
             '- 每道题的时限为初始建议，需要儿童实际体验后校准；不宣称已经由教师逐题审校。',
             '- 全部标准答案使用精确分数验算；判断题检查陈述，图形题检查单位。自动验算不等于题干、教学顺序和情境均已完成教学审核。',
             '- 外部题库（包括GSM8K）本批未导入，避免未经审核的机器翻译和混合授权。','']
    outputs={path:payload,ROOT/'docs/question-bank-coverage.md':'\n'.join(report)}
    for p,text in outputs.items():
        if args.check:
            assert p.read_text()==text,f'{p}与生成源不一致'
        else:p.parent.mkdir(parents=True,exist_ok=True);p.write_text(text)
    print(f'Validated {len(topics)} topics, {sum(len(set(q["TemplateId"] for q in t["Questions"])) for t in topics)} templates, {sum(len(t["Questions"])*2 for t in topics)} questions across 12 semesters.')


if __name__=='__main__':main()
