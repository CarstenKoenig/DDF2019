output/slides.html: output/reveal.js talk.md
	mkdir -p output
	pandoc -t revealjs --from markdown+tex_math_dollars --slide-level=2 -s -o output/slides.html talk.md -V revealjs-url=./reveal.js -V theme=beige

output/reveal.js:
	mkdir -p output
	cd ./output && \
		wget https://github.com/hakimel/reveal.js/archive/master.tar.gz && \
		tar -xzvf master.tar.gz && \
		rm -rf master.tar.gz && \
		mv reveal.js-master reveal.js

clean:
	rm -rf output