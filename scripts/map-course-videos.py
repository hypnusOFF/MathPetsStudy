#!/usr/bin/env python3
"""Conservative title matches; source metadata and unknown matches remain visible."""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[1]
parts=json.loads((root/'src/MathPet.Core/Content/video-parts.json').read_text())
topics=json.loads((root/'src/MathPet.Core/Content/question-bank.json').read_text())
def norm(text):return re.sub(r'[^\u4e00-\u9fff0-9]','',text)
aliases={'整时与分钟的初步认识':['认识钟表'],'元与角':['认识人民币'],'角与分':['认识人民币'],'平面图形的边与顶点':['认识图形'],'正反比例':['比例'],'1～5的认识与组成':['15的认识'],'十与一及数的组成':['1120各数'],'米与厘米':['长度单位'],'数列与图案数量规律':['找规律']}
aliases.update({'图形分类操作':['分类'],'拨钟面与时分':['认识时间','认识钟表','时分秒'],'观察积木视图':['观察物体'],'旋转图形操作':['旋转'],'方向与角度定位':['位置与方向'],'测量与综合实践':['周长'],
'前后左右与位置':['上下前后','左右'],'6～10的认识与比较':['6和7','8和9','10的认识'],
'平面图形的边与顶点':['认识平面图形','认识图形','角的认识'],'长方形与正方形的特征':['认识平面图形'],
'分类计数与简单统计':['分类','数据收集'],'数列与图案数量规律':['找规律'],
'米与厘米':['米','厘米'],'两位数加两位数':['两位数加','笔算加法'],'两位数减两位数':['两位数减','笔算减法'],
'长方体的棱与顶点':['长方体的认识','长方体和正方体的认识'],'时与分及经过时间':['认识时间','时间单位','经过时间'],
'简单排列与有序计数':['搭配'],'统计表与数量比较':['数据收集'],'乘除加减与小括号':['混合运算'],
'质量单位换算':['克和千克'],'等差规律与推理':['推理'],'分与秒':['认识秒','时间单位'],
'厘米与毫米':['毫米'],'千米与米':['千米'],'吨与千克':['吨'],'几倍与相同数量相加':['倍的认识'],
'大小月、平年与闰年':['年月日','年、月、日','平年','闰年'],'公顷与平方米':['公顷'],
'乘法笔算与应用':['笔算乘法'],'平面图形的特征':['平行四边形','梯形'],'合理安排时间':['优化'],
'带括号的四则运算':['四则运算'],'分配律与简便计算':['乘法分配律','运算定律'],
'立方分米与立方厘米':['体积单位'],'升与毫升':['容积'],'比的化简与意义':['比的意义','比的基本性质','化简比'],
'负数意义与大小比较':['负数'],'抽屉原理':['鸽巢'],'路程、速度与时间':['总复习','整理与复习'],
'单价、数量与总价':['总复习','整理与复习'],'直角特征与角的大小':['直角','角的认识'],
'从不同方向观察正方体':['观察物体'],'搭配、排列与组合':['搭配'],'旋转与大小不变':['旋转'],'按比分配':['比的应用']})
visual=[(1,1,'classify','图形分类操作','分类与整理'),(2,0,'clock','拨钟面与时分','认识时间'),(3,1,'bars','条形统计图读数','统计'),(4,0,'angle','量角与画角操作','角的度量'),(4,1,'reflect','在方格上画对称点','图形的运动'),(5,1,'rotate','旋转图形操作','图形的运动'),(6,0,'pie','扇形统计图读数','统计'),(6,1,'ring','圆环面积','圆'),(6,1,'proportion','正反比例图像','比例'),(4,1,'direction','方向与角度定位','位置与方向'),(2,0,'views','观察积木视图','观察物体'),(5,1,'practice','测量与综合实践','综合与实践')]
for id,g,se,n,u in [('addition',1,0,'10以内的加减法','加减法'),('multiply',2,0,'表内乘法','乘法'),('area',3,1,'长方形的面积','面积'),('decimal',4,1,'小数加减法','小数'),('fraction',5,1,'异分母分数加法','分数'),('percent',6,0,'百分数应用','百分数')]:topics.append(dict(Id=id,Grade=g,Semester=se,Name=n,UnitName=u))
for g,se,m,n,u in visual:topics.append(dict(Id='visual-'+m,Grade=g,Semester=se,Name=n,UnitName=u))
for p in parts:p['version_group']='标题标2026新结构' if p['label']=='三上' and p['page']<=42 else '上传者标旧版目录' if p['label']=='三上' else '待核教材版次'
aliases.update({'合理安排时间':['烙饼','沏茶'],'带括号的四则运算':['括号'],'图形分类操作':['标准分类','数据分类'],'测量与综合实践':['综合与实践'],'方向与角度定位':['图形与位置'],'路程、速度与时间':['常见的数量关系'],'单价、数量与总价':['常见的数量关系']})
catalog=[]
keys=['加法','减法','乘法','除法','分数','小数','比例','百分数','长方形','正方形','三角形','圆柱','圆锥','面积','体积','容积','时分秒','钟表','位置','方向','对称','旋转','统计','平均数','可能性','规律','人民币','因数','倍数','数位','分类','集合','鸡兔','数与形','植树','烙饼','优化','次品','搭配','周长','角度','角的','认识角','观察物体','找规律','十以内','退位','进位','简便','四则','数的组成','数的认识']
for t in topics:
 label='一二三四五六'[t['Grade']-1]+('上' if t['Semester']==0 else '下')
 terms=[norm(t['Name']),norm(t['UnitName']) if t['UnitName'] not in ['数学广角','整理与复习','综合与实践'] else '',*map(norm,aliases.get(t['Name'],[]))]
 terms.extend(norm(s) for s in re.split('[与及、]',t['Name']) if len(norm(s))>=3 and s not in ['意义','性质','认识','比较','生活应用'])
 domain=[k for k in keys if k in norm(t['Name']+t['UnitName'])]
 matched=[]
 for p in parts:
  if p['label']!=label or not p['anonymous_full_duration_available']:continue
  # 三上新结构与传统结构混放；当前知识点只匹配未带新版年份的目录。
  if label=='三上' and p['page']<=42:continue
  title=norm(p['title'])
  if any(term and term in title for term in terms) or any(k in title for k in domain):matched.append(p)
 matched.sort(key=lambda p:(p['page'] if label!='六上' else (0 if p['page']==2 else 1 if p['page']==1 else p['page'])))
 cross=False
 if not matched:
  # A different semester is only a clearly labelled related-topic link, never textbook alignment.
  candidates=[p for p in parts if p['anonymous_full_duration_available'] and p['label']!=label and any(term and (len(term)>=3 or term in ['搭配','烙饼','沏茶','括号','规律']) and term in norm(p['title']) for term in terms)]
  if candidates:matched=candidates[:12];cross=True
 catalog.append(dict(TopicId=t['Id'],Videos=[dict(Title='【'+p['label']+'·'+p['version_group']+'】'+p['title'],Url=f'https://www.bilibili.com/video/{p["bvid"]}/?p={p["page"]}',Page=p['page'],Bvid=p['bvid']) for p in matched],Status=('跨册相关主题候选，请按实际课题选择；未逐集教学审核' if cross else '知识点或单元主题相关候选，未逐集教学审核') if matched else '未找到明确标题匹配'))
(root/'src/MathPet.Core/Content/course-videos.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2)+'\n')
missing=[t['TopicId'] for t in catalog if not t['Videos']]
(root/'docs/research/video-mapping-status.md').write_text('# 视频挂接状态\n\n已保留701条匿名可播分集元数据。标题或主题匹配只作候选，不宣称教材版次或逐知识点内容已人工审核。三上P1–P42是标题标2026新结构，P43–P100上传者标旧版目录；当前基础目录只匹配P43以后；六上将P2排在P1前。\n\n'+f'156个题库、兼容知识点与图形练习知识点中{len(catalog)-len(missing)}个有候选，{len(missing)}个无明确标题匹配。无同册匹配时仅挂明确课题的跨年级/跨学期候选，并单独标注。没有候选时不自动跳到整册第一课。\n\n未匹配ID：'+', '.join(missing)+'\n')
print(f'{len(catalog)-len(missing)}/{len(catalog)} topics have explicit title candidates; {len(parts)} source parts preserved.')
