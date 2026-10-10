#!/usr/bin/env python3
"""Audit the shipped JSON bank independently of its template generator.

Exact expression checks cover every answer; text checks recompute recognizable
stems without trusting Calculation. Neither check replaces teacher review.
"""
import ast
import json
import re
from collections import Counter
from fractions import Fraction
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def calculate(expression):
    def visit(node):
        if isinstance(node, ast.Constant) and type(node.value) is int:
            return Fraction(node.value)
        if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.USub):
            return -visit(node.operand)
        if isinstance(node, ast.BinOp):
            a, b = visit(node.left), visit(node.right)
            if isinstance(node.op, ast.Add): return a + b
            if isinstance(node.op, ast.Sub): return a - b
            if isinstance(node.op, ast.Mult): return a * b
            if isinstance(node.op, ast.Div): return a / b
            if isinstance(node.op, ast.FloorDiv): return Fraction(a // b)
            if isinstance(node.op, ast.Mod): return a % b
            if isinstance(node.op, ast.Pow) and b.denominator == 1 and 0 <= b <= 3: return a ** int(b)
        raise ValueError(expression)
    return visit(ast.parse(expression, mode="eval").body)


def text_answer(q):
    text = q['Text']
    numeric = r'(\d+(?:\.\d+)?(?:/\d+)?)'
    match = re.fullmatch(numeric + r' ([+＋−×÷]) ' + numeric + r' = \?(?:（输入分数）)?', text)
    if match:
        a, op, b = match.groups(); a, b = Fraction(a), Fraction(b)
        return {'+': lambda: a+b, '＋': lambda: a+b, '−': lambda: a-b,
                '×': lambda: a*b, '÷': lambda: a/b}[op]()
    match = re.fullmatch(numeric + r' ([+＋−×÷]) ' + numeric + r' = ' + numeric + r'（正确选1，错误选0）', text)
    if match:
        a, op, b, claim = match.groups(); a, b, claim = Fraction(a), Fraction(b), Fraction(claim)
        value = {'+': lambda:a+b, '＋': lambda:a+b, '−': lambda:a-b,
                 '×': lambda:a*b, '÷': lambda:a/b}[op]()
        return Fraction(int(value == claim))
    match = re.fullmatch(r'□ ([+−×÷]) ' + numeric + r' = ' + numeric + r'，□里填多少？', text)
    if match:
        op, b, result = match.groups(); b, result = Fraction(b), Fraction(result)
        return {'+': lambda: result-b, '−': lambda: result+b,
                '×': lambda: result/b, '÷': lambda: result*b}[op]()
    match = re.fullmatch(r'把(\d+)/(\d+)化为最简分数。', text)
    if match: return Fraction(int(match[1]), int(match[2]))
    match = re.fullmatch(r'把(\d+(?:\.\d+)?)化为最简分数。', text)
    if match: return Fraction(match[1])
    match = re.fullmatch(r'(\d+)(\S+) = 多少(\S+)？', text)
    if match:
        conversions = {('米','厘米'):100,('厘米','毫米'):10,('千米','米'):1000,
                       ('元','角'):10,('角','分'):10,('千克','克'):1000,('吨','千克'):1000,
                       ('平方米','平方分米'):100,('平方分米','平方厘米'):100,
                       ('立方分米','立方厘米'):1000,('升','毫升'):1000,('分钟','秒'):60}
        count, source, target = match.groups()
        if (source,target) in conversions: return Fraction(int(count)*conversions[source,target])
        if (target,source) in conversions: return Fraction(int(count),conversions[target,source])
    match = re.fullmatch(r'按顺序填数：(\d+)、□、(\d+)。',text)
    if match:
        assert int(match[2])-int(match[1])==2,text
        return Fraction(int(match[1])+1)
    # Infer the formula from the mathematical shape described in the stem.
    value = None
    match = re.search(r'长(\d+)厘米、宽(\d+)厘米的长方形(周长|面积)',text)
    if match:
        a,b=int(match[1]),int(match[2]);value=2*(a+b) if match[3]=='周长' else a*b
    match = re.search(r'边长(\d+)厘米的正方形(周长|面积)',text)
    if match:
        a=int(match[1]);value=4*a if match[2]=='周长' else a*a
    match = re.search(r'底(\d+)厘米、对应高(\d+)厘米的(平行四边形|三角形)面积',text)
    if match:
        a,h=int(match[1]),int(match[2]);value=Fraction(a*h,2 if match[3]=='三角形' else 1)
    match = re.search(r'上底(\d+)厘米、下底(\d+)厘米、高(\d+)厘米的梯形面积',text)
    if match: value=Fraction((int(match[1])+int(match[2]))*int(match[3]),2)
    match = re.search(r'半径(\d+)厘米的圆的(周长|面积)',text)
    if match:
        r=int(match[1]);value=Fraction(314,100)*(2*r if match[2]=='周长' else r*r)
    match = re.search(r'长(\d+)厘米、宽(\d+)厘米、高(\d+)厘米的长方体(体积|表面积)',text)
    if match:
        a,b,h=map(int,match.group(1,2,3));value=a*b*h if match[4]=='体积' else 2*(a*b+a*h+b*h)
    match = re.search(r'棱长(\d+)厘米的正方体(体积|表面积)',text)
    if match:
        a=int(match[1]);value=a**3 if match[2]=='体积' else 6*a*a
    match = re.search(r'底面半径(\d+)厘米、高(\d+)厘米的(圆柱体积|圆锥体积|封闭圆柱表面积)',text)
    if match:
        r,h=int(match[1]),int(match[2]);pi=Fraction(314,100)
        value={'圆柱体积':pi*r*r*h,'圆锥体积':pi*r*r*h/3,'封闭圆柱表面积':2*pi*r*r+2*pi*r*h}[match[3]]
    if value is not None:
        if q['QuestionType']=='公式计算': return Fraction(value)
        if q['QuestionType']=='多步应用' and '做两个完全相同' in text: return Fraction(value*2)
        if q['QuestionType']=='判断辨析':
            claim=re.search(r'是(\d+(?:\.\d+)?(?:/\d+)?)(?:平方厘米|立方厘米|厘米)（正确选1',text)
            if claim: return Fraction(int(Fraction(claim[1])==value))
        if q['QuestionType']=='单位辨析':
            unit='立方厘米' if '体积' in text else '平方厘米' if '面积' in text else '厘米'
            return Fraction(q['Options'].index(unit))
    return None


def main():
    topics = json.loads((ROOT/'src/MathPet.Core/Content/question-bank.json').read_text())
    seen = set(); counts = Counter(); text_checked = 0
    for topic in topics:
        assert len(topic['Questions']) == len(topic['ReviewQuestions']), topic['Id']
        signatures = set()
        for original, review in zip(topic['Questions'],topic['ReviewQuestions']):
            assert original['TemplateId'] == review['TemplateId'], original['Id']
            for q in (original,review):
                answer = Fraction(q['Answer'])
                assert answer == calculate(q['Calculation']), q['Id']
                assert q['Id'] not in seen, q['Id']; seen.add(q['Id'])
                signature = (q['Text'], q['Answer'])
                assert signature not in signatures, q['Id']; signatures.add(signature)
                assert 30 <= q['TimeLimitSeconds'] <= 120 and len(q['Hints']) >= 2 and len(q['Solution']) >= 3, q['Id']
                assert q['Source'] == '独立原创', q['Id']
                if q['Options']: assert answer.denominator == 1 and 0 <= answer < len(q['Options']), q['Id']
                expected = text_answer(q)
                if expected is not None:
                    assert answer == expected, (q['Id'],q['Text']); text_checked += 1
                if '最简分数' in q['Text']:
                    assert q['Answer'] == str(answer), q['Id']
                    assert '/' not in q['Answer'] or Fraction(q['Answer']).denominator == int(q['Answer'].split('/')[1]), q['Id']
                if topic['Name'] == '整时的认识':
                    assert '分钟' not in q['Text'] and (1 <= answer <= 12 or q['QuestionType'] == '判断辨析'), q['Id']
                if topic['Grade'] == 1 and topic['Semester'] == 0 and topic['Name'] == '前后左右与位置':
                    assert '*' not in q['Calculation'] and answer <= 20, q['Id']
                if topic['Name'] == '平均分与除法口诀' and q['QuestionType'] == '直接计算':
                    assert 1 <= answer <= 9, q['Id']
                if topic['Name'] == '6～10的认识与比较' and q['QuestionType'] == '大小比较':
                    assert 6 <= answer <= 10, q['Id']
                counts[f'{topic["Grade"]}{"上" if topic["Semester"]==0 else "下"}'] += 1
    result = {'main_topics':len(topics),'main_questions':len(seen),
              'exact_expression_checked':len(seen),'independent_stem_checked':text_checked,
              'by_semester':dict(sorted(counts.items()))}
    output=ROOT/'artifacts/question-bank-audit.json'; output.parent.mkdir(exist_ok=True)
    output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(result,ensure_ascii=False))


if __name__ == '__main__': main()
