threshold1 = 0.35;
threshold2 = 1.22;

a1 = pi*(0:180)/180 - pi/2;
a2 = pi*(0:180)/180 - pi/2;
G0 = zeros(numel(a1));
G1 = zeros(numel(a1));
G2 = zeros(numel(a1));
AG = zeros(numel(a1));

for i=1:numel(a1)
    for j=1:numel(a2)
        if abs(a1(i) - a2(j)) > threshold1
            G0(i,j) = exp(-12 * (abs(a1(i) - a2(j)) - threshold1));
        else
            G0(i,j) = 1;
        end
        
        if abs(a1(i)) > threshold2
            G1(i,j) = exp(-12 * (abs(a1(i)) - threshold2));
        else
            G1(i,j) = 1;
        end
        
        if abs(a2(j)) > threshold2
            G2(i,j) = exp(-12 * (abs(a2(j)) - threshold2));
        else
            G2(i,j) = 1;
        end
        AG(i,j) = G0(i,j) * G1(i,j) * G2(i,j);
    end
end

figure
h=pcolor(10*log10(AG.^2));
set(h,'linestyle','none')
caxis([-50, 0])
axis equal